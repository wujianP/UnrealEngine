// Copyright 1998-2015 Epic Games, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;

namespace UnrealBuildTool
{
	public enum ModuleHostType
	{
		Runtime,
		RuntimeNoCommandlet,
		Developer,
		Editor,
		EditorNoCommandlet,
		Program,
	}

	public enum ModuleLoadingPhase
	{
		Default,
		PostDefault,
		PreDefault,
		PostConfigInit,
		PreLoadingScreen,
		PostEngineInit,
	}

	[DebuggerDisplay("Name={Name}")]
	public class ModuleDescriptor
	{
		// Name of this module
		public readonly string Name;

		// Usage type of module
		public ModuleHostType Type;

		// When should the module be loaded during the startup sequence?  This is sort of an advanced setting.
		public ModuleLoadingPhase LoadingPhase = ModuleLoadingPhase.Default;

		// List of allowed platforms
		public UnrealTargetPlatform[] WhitelistPlatforms;

		// List of disallowed platforms
		public UnrealTargetPlatform[] BlacklistPlatforms;

		// List of additional dependencies for building this module.
		public string[] AdditionalDependencies;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="InName">Name of the module</param>
		/// <param name="InType">Type of target that can host this module</param>
		public ModuleDescriptor(string InName, ModuleHostType InType)
		{
			Name = InName;
			Type = InType;
		}

		/// <summary>
		/// Constructs a ModuleDescriptor from a Json object
		/// </summary>
		/// <param name="InObject"></param>
		/// <returns>The new module descriptor</returns>
		public static ModuleDescriptor FromJsonObject(JsonObject InObject)
		{
			ModuleDescriptor Module = new ModuleDescriptor(InObject.GetStringField("Name"), InObject.GetEnumField<ModuleHostType>("Type"));

			ModuleLoadingPhase LoadingPhase;
			if(InObject.TryGetEnumField<ModuleLoadingPhase>("LoadingPhase", out LoadingPhase))
			{
				Module.LoadingPhase = LoadingPhase;
			}

			UnrealTargetPlatform[] WhitelistPlatforms;
			if(InObject.TryGetEnumArrayField<UnrealTargetPlatform>("WhitelistPlatforms", out WhitelistPlatforms))
			{
				Module.WhitelistPlatforms = WhitelistPlatforms;
			}

			UnrealTargetPlatform[] BlacklistPlatforms;
			if(InObject.TryGetEnumArrayField<UnrealTargetPlatform>("BlacklistPlatforms", out BlacklistPlatforms))
			{
				Module.BlacklistPlatforms = BlacklistPlatforms;
			}

			string[] AdditionalDependencies;
			if(InObject.TryGetStringArrayField("AdditionalDependencies", out AdditionalDependencies))
			{
				Module.AdditionalDependencies = AdditionalDependencies;
			}

			return Module;
		}

		/// <summary>
		/// Write this module to a JsonWriter
		/// </summary>
		/// <param name="Writer">Writer to output to</param>
		void Write(JsonWriter Writer)
		{
			Writer.WriteObjectStart();
			Writer.WriteValue("Name", Name);
			Writer.WriteValue("Type", Type.ToString());
			Writer.WriteValue("LoadingPhase", LoadingPhase.ToString());
			if (WhitelistPlatforms != null && WhitelistPlatforms.Length > 0)
			{
				Writer.WriteArrayStart("WhitelistPlatforms");
				foreach(UnrealTargetPlatform WhitelistPlatform in WhitelistPlatforms)
				{
					Writer.WriteValue(WhitelistPlatform.ToString());
				}
				Writer.WriteArrayEnd();
			}
			if (BlacklistPlatforms != null && BlacklistPlatforms.Length > 0)
			{
				Writer.WriteArrayStart("BlacklistPlatforms");
				foreach(UnrealTargetPlatform BlacklistPlatform in BlacklistPlatforms)
				{
					Writer.WriteValue(BlacklistPlatform.ToString());
				}
				Writer.WriteArrayEnd();
			}
			if (AdditionalDependencies != null && AdditionalDependencies.Length > 0)
			{
				Writer.WriteArrayStart("AdditionalDependencies");
				foreach(string AdditionalDependency in AdditionalDependencies)
				{
					Writer.WriteValue(AdditionalDependency);
				}
				Writer.WriteArrayEnd();
			}
			Writer.WriteObjectEnd();
		}

		/// <summary>
		/// Write an array of module descriptors
		/// </summary>
		/// <param name="Writer">The Json writer to output to</param>
		/// <param name="Name">Name of the array</param>
		/// <param name="Modules">Array of modules</param>
		public static void WriteArray(JsonWriter Writer, string Name, ModuleDescriptor[] Modules)
		{
			if(Modules.Length > 0)
			{
				Writer.WriteArrayStart(Name);
				foreach(ModuleDescriptor Module in Modules)
				{
					Module.Write(Writer);
				}
				Writer.WriteArrayEnd();
			}
		}

		/// <summary>
		/// Determines whether the given plugin module is part of the current build.
		/// </summary>
		/// <param name="Platform">The platform being compiled for</param>
		/// <param name="TargetType">The type of the target being compiled</param>
		public bool IsCompiledInConfiguration(UnrealTargetPlatform Platform, TargetRules.TargetType TargetType)
		{
			// Check the platform is whitelisted
			if(WhitelistPlatforms != null && WhitelistPlatforms.Length > 0 && !WhitelistPlatforms.Contains(Platform))
			{
				return false;
			}

			// Check the platform is not blacklisted
			if(BlacklistPlatforms != null && BlacklistPlatforms.Contains(Platform))
			{
				return false;
			}

			// Check the module is compatible with this target.
			switch (Type)
			{
				case ModuleHostType.Runtime:
				case ModuleHostType.RuntimeNoCommandlet:
					return true;
				case ModuleHostType.Developer:
					return UEBuildConfiguration.bBuildDeveloperTools;
				case ModuleHostType.Editor:
				case ModuleHostType.EditorNoCommandlet:
					return TargetType == TargetRules.TargetType.Editor || UEBuildConfiguration.bBuildEditor;
				case ModuleHostType.Program:
					return TargetType == TargetRules.TargetType.Program;
			}

			return false;
		}
	}
}
