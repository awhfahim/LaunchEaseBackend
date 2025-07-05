namespace Common.Application.Misc;

public enum ErrorType
{
    BadRequest = 1,
    Unauthorized = 2,
    PaymentRequired = 3,
    Forbidden = 4,
    NotFound = 5,
    MethodNotAllowed = 6,
    NotAcceptable = 7,
    ProxyAuthenticationRequired = 8,
    RequestTimeout = 9,
    Conflict = 10, // 0x0A
    Gone = 11, // 0x0B
    LengthRequired = 12, // 0x0C
    PreconditionFailed = 13, // 0x0D
    PayloadTooLarge = 14, // 0x0E
    UriTooLong = 15, // 0x0F
    UnsupportedMediaType = 16, // 0x10
    RangeNotSatisfiable = 17, // 0x11
    ExpectationFailed = 18, // 0x12
    ImATeapot = 19, // 0x13
    MisdirectedRequest = 20, // 0x14
    UnprocessableEntity = 21, // 0x15
    Locked = 22, // 0x16
    FailedDependency = 23, // 0x17
    TooEarly = 24, // 0x18
    UpgradeRequired = 25, // 0x19
    PreconditionRequired = 26, // 0x1A
    TooManyRequests = 27, // 0x1B
    RequestHeaderFieldsTooLarge = 28, // 0x1C
    UnavailableForLegalReasons = 29, // 0x1D
    InternalServerError = 30, // 0x1E
    NotImplemented = 31, // 0x1F
    BadGateway = 32, // 0x20
    ServiceUnavailable = 33, // 0x21
    GatewayTimeout = 34, // 0x22
    HttpVersionNotSupported = 35, // 0x23
    VariantAlsoNegotiates = 36, // 0x24
    InsufficientStorage = 37, // 0x25
    LoopDetected = 38, // 0x26
    NotExtended = 39, // 0x27
    NetworkAuthenticationRequired = 40, // 0x28,
    Validation = 41,
}
