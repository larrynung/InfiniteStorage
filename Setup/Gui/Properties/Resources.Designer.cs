﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18034
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Gui.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Gui.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Drawing.Bitmap.
        /// </summary>
        internal static System.Drawing.Bitmap BonjourInstallationStepImg {
            get {
                object obj = ResourceManager.GetObject("BonjourInstallationStepImg", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Setup is configuring bonjour service....
        /// </summary>
        internal static string ConfigureBonjourService {
            get {
                return ResourceManager.GetString("ConfigureBonjourService", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The wizard has detected that newer version of Favorite Home is already installed on your computer. Setup cannot continue..
        /// </summary>
        internal static string DowngradeNotSupported {
            get {
                return ResourceManager.GetString("DowngradeNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0}\application.exe.
        /// </summary>
        internal static string FinishStep1Command {
            get {
                return ResourceManager.GetString("FinishStep1Command", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0}\InfiniteStorage.exe.
        /// </summary>
        internal static string FinishStepCommand {
            get {
                return ResourceManager.GetString("FinishStepCommand", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Drawing.Bitmap.
        /// </summary>
        internal static System.Drawing.Bitmap InstallationStepImg {
            get {
                object obj = ResourceManager.GetObject("InstallationStepImg", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Setup is installing bonjour service....
        /// </summary>
        internal static string InstallBonjourService {
            get {
                return ResourceManager.GetString("InstallBonjourService", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Package.msi.
        /// </summary>
        internal static string MainMsiFile {
            get {
                return ResourceManager.GetString("MainMsiFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The wizard will install Favorite Home on your computer..
        /// </summary>
        internal static string WelcomeStepGreetingInstall {
            get {
                return ResourceManager.GetString("WelcomeStepGreetingInstall", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The wizard will modify Favorite Home on your computer..
        /// </summary>
        internal static string WelcomeStepGreetingModify {
            get {
                return ResourceManager.GetString("WelcomeStepGreetingModify", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The wizard will reinstall Favorite Home on your computer..
        /// </summary>
        internal static string WelcomeStepGreetingReinstall {
            get {
                return ResourceManager.GetString("WelcomeStepGreetingReinstall", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The wizard will uninstall Favorite Home from your computer..
        /// </summary>
        internal static string WelcomeStepGreetingUninstall {
            get {
                return ResourceManager.GetString("WelcomeStepGreetingUninstall", resourceCulture);
            }
        }
    }
}
