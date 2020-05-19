﻿namespace WUApiLib
{
    using System;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [ComImport, TypeLibType((short) 0x10c0), DefaultMember("Title"), Guid("004C6A2B-0C19-4C69-9F5C-A269B2560DB9")]
    public interface IWindowsDriverUpdate4 : IWindowsDriverUpdate3
    {
        [DispId(0)]
        string Title { [return: MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0)] get; }
        [DispId(0x60020001)]
        bool AutoSelectOnWebSites { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60020001)] get; }
        [DispId(0x60020002)]
        UpdateCollection BundledUpdates { [return: MarshalAs(UnmanagedType.Interface)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60020002)] get; }
        [DispId(0x60020003)]
        bool CanRequireSource { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60020003)] get; }
        [DispId(0x60020004)]
        ICategoryCollection Categories { [return: MarshalAs(UnmanagedType.Interface)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60020004)] get; }
        [DispId(0x60020005)]
        object Deadline { [return: MarshalAs(UnmanagedType.Struct)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60020005)] get; }
        [DispId(0x60020006)]
        bool DeltaCompressedContentAvailable { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60020006)] get; }
        [DispId(0x60020007)]
        bool DeltaCompressedContentPreferred { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60020007)] get; }
        [DispId(0x60020008)]
        string Description { [return: MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60020008)] get; }
        [DispId(0x60020009)]
        bool EulaAccepted { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60020009)] get; }
        [DispId(0x6002000a)]
        string EulaText { [return: MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x6002000a)] get; }
        [DispId(0x6002000b)]
        string HandlerID { [return: MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x6002000b)] get; }
        [DispId(0x6002000c)]
        IUpdateIdentity Identity { [return: MarshalAs(UnmanagedType.Interface)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x6002000c)] get; }
        [DispId(0x6002000d)]
        IImageInformation Image { [return: MarshalAs(UnmanagedType.Interface)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x6002000d)] get; }
        [DispId(0x6002000e)]
        IInstallationBehavior InstallationBehavior { [return: MarshalAs(UnmanagedType.Interface)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x6002000e)] get; }
        [DispId(0x6002000f)]
        bool IsBeta { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x6002000f)] get; }
        [DispId(0x60020010)]
        bool IsDownloaded { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60020010)] get; }
        [DispId(0x60020011)]
        bool IsHidden { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60020011)] get; [param: In] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60020011)] set; }
        [DispId(0x60020012)]
        bool IsInstalled { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60020012)] get; }
        [DispId(0x60020013)]
        bool IsMandatory { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60020013)] get; }
        [DispId(0x60020014)]
        bool IsUninstallable { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60020014)] get; }
        [DispId(0x60020015)]
        StringCollection Languages { [return: MarshalAs(UnmanagedType.Interface)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60020015)] get; }
        [DispId(0x60020016)]
        DateTime LastDeploymentChangeTime { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60020016)] get; }
        [DispId(0x60020017)]
        decimal MaxDownloadSize { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60020017)] get; }
        [DispId(0x60020018)]
        decimal MinDownloadSize { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60020018)] get; }
        [DispId(0x60020019)]
        StringCollection MoreInfoUrls { [return: MarshalAs(UnmanagedType.Interface)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60020019)] get; }
        [DispId(0x6002001a)]
        string MsrcSeverity { [return: MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x6002001a)] get; }
        [DispId(0x6002001b)]
        int RecommendedCpuSpeed { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x6002001b)] get; }
        [DispId(0x6002001c)]
        int RecommendedHardDiskSpace { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x6002001c)] get; }
        [DispId(0x6002001d)]
        int RecommendedMemory { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x6002001d)] get; }
        [DispId(0x6002001e)]
        string ReleaseNotes { [return: MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x6002001e)] get; }
        [DispId(0x6002001f)]
        StringCollection SecurityBulletinIDs { [return: MarshalAs(UnmanagedType.Interface)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x6002001f)] get; }
        [DispId(0x60020021)]
        StringCollection SupersededUpdateIDs { [return: MarshalAs(UnmanagedType.Interface)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60020021)] get; }
        [DispId(0x60020022)]
        string SupportUrl { [return: MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60020022)] get; }
        [ComAliasName("WUApiLib.UpdateType"), DispId(0x60020023)]
        UpdateType Type { [return: ComAliasName("WUApiLib.UpdateType")] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60020023)] get; }
        [DispId(0x60020024)]
        string UninstallationNotes { [return: MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60020024)] get; }
        [DispId(0x60020025)]
        IInstallationBehavior UninstallationBehavior { [return: MarshalAs(UnmanagedType.Interface)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60020025)] get; }
        [DispId(0x60020026)]
        StringCollection UninstallationSteps { [return: MarshalAs(UnmanagedType.Interface)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60020026)] get; }
        [DispId(0x60020028)]
        StringCollection KBArticleIDs { [return: MarshalAs(UnmanagedType.Interface)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60020028)] get; }
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60020027)]
        void AcceptEula();
        [ComAliasName("WUApiLib.DeploymentAction"), DispId(0x60020029)]
        WUApiLib.DeploymentAction DeploymentAction { [return: ComAliasName("WUApiLib.DeploymentAction")] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60020029)] get; }
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x6002002a)]
        void CopyFromCache([In, MarshalAs(UnmanagedType.BStr)] string path, [In] bool toExtractCabFiles);
        [ComAliasName("WUApiLib.DownloadPriority"), DispId(0x6002002b)]
        WUApiLib.DownloadPriority DownloadPriority { [return: ComAliasName("WUApiLib.DownloadPriority")] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x6002002b)] get; }
        [DispId(0x6002002c)]
        IUpdateDownloadContentCollection DownloadContents { [return: MarshalAs(UnmanagedType.Interface)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x6002002c)] get; }
        [DispId(0x60030001)]
        string DriverClass { [return: MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60030001)] get; }
        [DispId(0x60030002)]
        string DriverHardwareID { [return: MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60030002)] get; }
        [DispId(0x60030003)]
        string DriverManufacturer { [return: MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60030003)] get; }
        [DispId(0x60030004)]
        string DriverModel { [return: MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60030004)] get; }
        [DispId(0x60030005)]
        string DriverProvider { [return: MarshalAs(UnmanagedType.BStr)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60030005)] get; }
        [DispId(0x60030006)]
        DateTime DriverVerDate { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60030006)] get; }
        [DispId(0x60030007)]
        int DeviceProblemNumber { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60030007)] get; }
        [DispId(0x60030008)]
        int DeviceStatus { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60030008)] get; }
        [DispId(0x60040001)]
        bool RebootRequired { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60040001)] get; }
        [DispId(0x60040003)]
        bool IsPresent { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60040003)] get; }
        [DispId(0x60040004)]
        StringCollection CveIDs { [return: MarshalAs(UnmanagedType.Interface)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60040004)] get; }
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60040002)]
        void CopyToCache([In, MarshalAs(UnmanagedType.Interface)] StringCollection pFiles);
        [DispId(0x60050001)]
        bool BrowseOnly { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60050001)] get; }
        [DispId(0x60060001)]
        IWindowsDriverUpdateEntryCollection WindowsDriverUpdateEntries { [return: MarshalAs(UnmanagedType.Interface)] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60060001)] get; }
        [DispId(0x60060002)]
        bool PerUser { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), DispId(0x60060002)] get; }
    }
}

