namespace Common.Domain.Enums;

public enum MatchablePermissionCategory : uint
{
    Moderation = 1,
    ModuleAccess,
    User,
    Role,
    Permission,

    // PFM
    CanAccessPrintingConfigurations,
    CanAccessPrintingResources,
    CanAccessPrintingTransactions,
    CanAccessPrintingReports,
    PrintingResource,

    // IGMT4
    CanAccessIgmt4Configurations,
    CanAccessIgmt4Resources,
    CanAccessIgmt4Transactions,
    CanAccessIgmt4Reports,
    ReferenceData,
    Igmt4MachineProfile
}
