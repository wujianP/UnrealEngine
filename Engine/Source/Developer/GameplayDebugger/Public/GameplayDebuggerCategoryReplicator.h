// Copyright 1998-2016 Epic Games, Inc. All Rights Reserved.

#pragma once

#include "GameplayDebuggerTypes.h"
#include "GameplayDebuggerCategoryReplicator.generated.h"

class APlayerController;
class UInputComponent;
class FGameplayDebuggerCategory;
class AGameplayDebuggerCategoryReplicator;
class UGameplayDebuggerRenderingComponent;

USTRUCT()
struct FGameplayDebuggerNetPack
{
	GENERATED_USTRUCT_BODY()

	AGameplayDebuggerCategoryReplicator* Owner;

	FGameplayDebuggerNetPack() : Owner(nullptr) {}
	bool NetDeltaSerialize(FNetDeltaSerializeInfo & DeltaParms);
	void OnCategoriesChanged();

private:
	struct FCategoryData
	{
		TArray<FString> TextLines;
		TArray<FGameplayDebuggerShape> Shapes;
		TArray<FGameplayDebuggerDataPack::FHeader> DataPacks;
		bool bIsEnabled;
	};
	TArray<FCategoryData> SavedData;
};

template<>
struct TStructOpsTypeTraits<FGameplayDebuggerNetPack> : public TStructOpsTypeTraitsBase
{
	enum
	{
		WithNetDeltaSerializer = true,
	};
};

USTRUCT()
struct FGameplayDebuggerDataPacketRequest
{
	GENERATED_USTRUCT_BODY()

	UPROPERTY()
	int32 CategoryId;

	UPROPERTY()
	int32 DataPackId;

	UPROPERTY()
	int32 DataVersion;

	UPROPERTY()
	int32 DataOffset;
};

UCLASS(NotBlueprintable, NotBlueprintType, notplaceable, noteditinlinenew, hidedropdown, Transient)
class GAMEPLAYDEBUGGER_API AGameplayDebuggerCategoryReplicator : public AActor
{
	GENERATED_UCLASS_BODY()

	virtual class UNetConnection* GetNetConnection() const override;
	virtual bool IsNetRelevantFor(const AActor* RealViewer, const AActor* ViewTarget, const FVector& SrcLocation) const override;
	virtual void PostNetReceive() override;
	virtual void BeginPlay() override;
	virtual void Destroyed() override;
	virtual void TickActor(float DeltaTime, enum ELevelTick TickType, FActorTickFunction& ThisTickFunction) override;

	/** [AUTH] set new owner */
	void SetReplicatorOwner(APlayerController* InOwnerPC);

	/** [ALL] set replicator state */
	void SetEnabled(bool bEnable);

	/** [ALL] set category state */
	void SetCategoryEnabled(int32 CategoryId, bool bEnable);

	/** [ALL] set actor for debugging */
	void SetDebugActor(AActor* Actor);

	/** get current debug actor */
	AActor* GetDebugActor() const { return DebugActor; }
	
	/** get player controller owning this replicator */
	APlayerController* GetReplicationOwner() const { return OwnerPC; }

	/** get replicator state */
	bool IsEnabled() const { return bIsEnabled; }

	/** get category state */
	bool IsCategoryEnabled(int32 CategoryId) const;

	/** get category count */
	int32 GetNumCategories() const { return Categories.Num(); }

	/** get category object */
	TSharedRef<FGameplayDebuggerCategory> GetCategory(int32 CategoryId) const { return Categories[CategoryId]; }

protected:

	friend FGameplayDebuggerNetPack;

	UPROPERTY(Replicated)
	APlayerController* OwnerPC;

	UPROPERTY(Replicated)
	AActor*	DebugActor;

	UPROPERTY(Replicated)
	bool bIsEnabled;

	UPROPERTY(Replicated)
	FGameplayDebuggerNetPack ReplicatedData;

	/** rendering component needs to attached to some actor, and this is as good as any */
	UPROPERTY()
	UGameplayDebuggerRenderingComponent* RenderingComp;

	/** category objects */
	TArray<TSharedRef<FGameplayDebuggerCategory> > Categories;

	/** pending data pack replication requests */
	TArray<FGameplayDebuggerDataPacketRequest> PendingReplicationRequests;

	uint32 bHasAuthority : 1;
	uint32 bIsLocal : 1;

	/** notify about changes in known category set */
	void OnCategoriesChanged();

	UFUNCTION(Server, Reliable, WithValidation, meta = (CallInEditor = "true"))
	void ServerSetEnabled(bool bEnable);

	UFUNCTION(Server, Reliable, WithValidation, meta = (CallInEditor = "true"))
	void ServerSetDebugActor(AActor* Actor);

	UFUNCTION(Server, Reliable, WithValidation, meta = (CallInEditor = "true"))
	void ServerSetCategoryEnabled(int32 CategoryId, bool bEnable);

	UFUNCTION(Server, Reliable, WithValidation, meta = (CallInEditor = "true"))
	void ServerRequestDataPackets(const TArray<FGameplayDebuggerDataPacketRequest>& RequestList);

	/** [LOCAL] notify from CategoryData replication */
	void OnReceivedDataPackPacket(int32 CategoryId, int32 DataPackId, const FGameplayDebuggerDataPack& DataPacket);
};