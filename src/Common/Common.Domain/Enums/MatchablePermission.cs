namespace Common.Domain.Enums;

/**
 1st level: Has***
 2nd level: Can***
 3rd level: ***
**/
public enum MatchablePermission : uint
{
    // Add Modules Here
    SuperAdmin = 1,
    HasAccessInAccessControlManagementModule,
    HasAccessInOrderTrackingSystemModule,
    HasAccessInProductLifetimeManagementModule,

    // User
    CanAccessUser,
    ReadUser,
    ReadUserStatus,
    UpdateUser,
    CreateUser,
    
    //Role
    CanAccessRole,
    ReadRole,
    CreateRole,
    UpdateRole,
    DeleteRole,
    UpdatePermissionOfUser,
    RemoveRoleFromUser,
    AssignRoleToUser,
    UpdateUserPassword,
    
    // Menu permission
    CanAccessArticle,
    CanAccessResourceConfig,
    CanAccessOtsConfig,
    CanAccessTnaSetup,
    
    //Article Gallery
    CanAccessArticleGallery,
    ReadArticleGallery,
    EditArticleGallery,
    
    //Article
    CanAccessArticleRegistration,
    ReadArticleRegistration,
    CreateArticleRegistration,
    UpdateArticleRegistration,
    
    //ArticleItem
    AccessArticleItem,
    ReadArticleItem,
    CreateArticleItem,
    UpdateArticleItem,
    
    //ArticleFabrication
    AccessFabrication,
    ReadFabrication,
    CreateFabrication,
    UpdateFabrication,
    
    //Color And Service
    AccessColorAndService,
    ReadColorAndService,
    CreateColorAndService,
    UpdateColorAndService,
    
    //ArticleOrder
    AccessOrderInformation,
    ReadOrderInformation,
    CreateOrderInformation,
    UpdateOrderInformation,
    CreateJob,
    UpdateJob,
    ReadJob,
    
    //ArticleOrderBreakdown
    AccessOrderBreakdown,
    ReadOrderBreakdown,
    CreateOrderBreakdown,
    
    //Tna
    AccessTna,
    ReadTna,
    CreateTna,
    GenerateTna,
    ResetTna,
    ReviseTna,
    
    //Yarn And Fabric Sourcing
    AccessYarnAndFabricSourcing,
    ReadYarnAndFabricSourcing,
    CreateYarnAndFabricSourcing,
    UpdateYarnAndFabricSourcing,
    UpdatePi,
    CreatePi,
    CreateFeedback,
    
    //Sales Contract
    AccessSalesContract,
    UpdateSalesContract,
    
    //Amendment
    AccessAmendment,
    LockAndUnlockOrder,
    UpdateAmendment,
    
    //Company
    CanAccessCompany,
    ReadCompany,
    CreateCompany,
    UpdateCompany,
    
    //Office
    CanAccessOffice,
    ReadOffice,
    CreateOffice,
    UpdateOffice,
    MapUserToOffice,
    
    //Supplier
    CanAccessSupplier,
    ReadSupplier,
    CreateSupplier,
    UpdateSupplier,
    MapMerchantToSupplier,
    MapPlmUserToSupplier,
    MapTypeToSupplier,
    
    //Yarn Count
    CanAccessYarnCount,
    ReadYarnCount,
    CreateYarnCount,
    UpdateYarnCount,
    
    //Yarn Item Specification
    CanAccessYarnItemSpecification,
    ReadYarnItemSpecification,
    CreateYarnItemSpecification,
    UpdateYarnItemSpecification,
    
    //Item Category
    CanAccessItemCategory,
    ReadItemCategory,
    CreateItemCategory,
    UpdateItemCategory,
    
    //Fiscal Year
    CanAccessFiscalYear,
    ReadFiscalYear,
    CreateFiscalYear,
    
    //Holiday Setup
    CanAccessHolidaySetup,
    ReadHolidaySetup,
    CreateHolidaySetup,
    UpdateHolidaySetup,
    
    //Buyer
    CanAccessBuyer,
    ReadBuyer,
    CreateBuyer,
    UpdateBuyer,
    
    //Buyer Agent
    CanAccessBuyerAgent,
    ReadBuyerAgent,
    CreateBuyerAgent,
    UpdateBuyerAgent,
    
    //Buyer Account
    CanAccessBuyerAccount,
    ReadBuyerAccount,
    CreateBuyerAccount,
    UpdateBuyerAccount,
    MapUserToBuyerAccount,
    
    //Fabric Type
    CanAccessFabricType,
    ReadFabricType,
    CreateFabricType,
    UpdateFabricType,
    
    //Color
    CanAccessColor,
    ReadColor,
    CreateColor,
    UpdateColor,
    
    //Size
    CanAccessSize,
    ReadSize,
    CreateSize,
    UpdateSize,
    
    //FiberComposition
    CanAccessFiberComposition,
    ReadFiberComposition,
    CreateFiberComposition,
    
    //Season
    CanAccessSeason,
    ReadSeason,
    CreateSeason,
    UpdateSeason,
    
    //Tna group Setup
    CanAccessTimeAndActionTaskGroup,
    ReadTimeAndActionTaskGroup,
    CreateTimeAndActionTaskGroup,
    UpdateTimeAndActionTaskGroup,
    
    //Tna Task
    CanAccessTimeAndActionTask,
    ReadTimeAndActionTask,
    CreateTimeAndActionTask,
    UpdateTimeAndActionTask,
    
    //Tna Template
    CanAccessTimeAndActionTemplate,
    ReadTimeAndActionTemplate,
    CreateTimeAndActionTemplate,
    UpdateTimeAndActionTemplate,
    
    //Tna Task Setup
    CanAccessTimeAndActionTaskSetup,
    ReadTimeAndActionTaskSetup,
    UpdateTimeAndActionTaskSetup,
    
    //Process Type
    CanAccessProcessType,
    ReadProcessType,
    CreateProcessType,
    UpdateProcessType,
    MapProcessWithTna,
    
    //Dashboard
    CanAccessOtsDashboard,
    ViewOrderStatus,
    ViewProductionFollowUp,
    ViewLcAndScStatus,
    AddTnaComment,
    AddTnaSampleComment,
    UpdateActualDate,
    
    //PLM
    //Textile
    CanAccessTextile,
    
    CanAccessYarn,
    UpdateYarn,
    CanAccessKnitting,
    UpdateKnitting,
    CanAccessDyeing,
    UpdateDyeing,
    CanAccessDyeingFinishing,
    UpdateDyeingFinishing,
    CanAccessFinishFabric,
    UpdateFinishFabric,
    
    //Garments
    CanAccessGarments,
    
    CanAccessCutting,
    UpdateCutting,
    CanAccessPrint,
    UpdatePrint,
    CanAccessEmbroidery,
    UpdateEmbroidery,
    CanAccessSewing,
    UpdateSewing,
    CanAccessWashDelivery,
    UpdateWashDelivery,
    CanAccessWashReceive,
    UpdateWashReceive,
    CanAccessPoly,
    UpdatePoly,
    CanAccessCarton,
    UpdateCarton,
    CanAccessShipment,
    UpdateShipment,
    
    //Notification Channel
    CanAccessNotificationChannel,
    ReadNotificationChannel,
    CreateNotificationChannel,
    UpdateNotificationChannel
}
