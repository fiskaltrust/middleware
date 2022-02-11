using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.de;
using DieboldNixdorfConfiguration = fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.DieboldNixdorfConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Commands;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Communication;
using Moq;
using Microsoft.Extensions.Logging;
using System;

namespace fiskaltrust.Middleware.SCU.DE.AcceptanceTest.SCU
{
    public class DieboldNixdorfDESSCDTests: IDESSCDTests
    {
        private IDESSCD _instance = null;

        private const string _DEVICE_PATH = "COM3";

        protected override string TseClientId => "POS001";

        protected override TseInfo ExpectedInitializedTseInfo => new TseInfo
        {
            CertificationIdentification = "0001000200010002",
            PublicKeyBase64 = "BEdFgaeSQUjdmCrwZm949fR9qgLXILs8ntrhx/OlRw2EPEmF1+8cZen8KDHqJPmGcTTVZch3CBHUsVVwQeKlPnKpwISSQQTzEJp3eEXRGualEPCYZ4RsWlYGP78WA1CItw==",
            SerialNumberOctet = "47F29FDAC9CC1FB6C8FED230BD6359E2273A5152E6CAC8EEDC49FB532F26660E",
            SignatureAlgorithm = "ecdsa-plain-SHA384",
            MaxNumberOfStartedTransactions = 512,
            MaxNumberOfSignatures = 20000000,
            MaxNumberOfClients = 100,
            MaxLogMemorySize = 6979321856,
            LogTimeFormat = "unixTime",
            FirmwareIdentification = "Format: SingleTSE; Firmware Version: 1.7.691; Loader Version: 1.1.497",
            CertificatesBase64 = new List<string>
            {
                "LS0tLS1CRUdJTiBDRVJUSUZJQ0FURS0tLS0tCk1JSURaakNDQXUyZ0F3SUJBZ0lRQU4wNW9PT0hDTW1MS1ZJVnhiNEI4ekFLQmdncWhrak9QUVFEQXpCbk1SWXcKRkFZRFZRUURFdzFVVTBVZ1ZHVnpkQ0JEUVNBeE1TVXdJd1lEVlFRS0V4eFVMVk41YzNSbGJYTWdTVzUwWlhKdQpZWFJwYjI1aGJDQkhiV0pJTVJrd0Z3WURWUVFMRXhCVVpXeGxhMjl0SUZObFkzVnlhWFI1TVFzd0NRWURWUVFHCkV3SkVSVEFlRncweU1EQTFNamN5TVRJd05EbGFGdzB5TURFeE1qY3lNelU1TlRsYU1JR0lNUmd3RmdZRFZRUXUKRXc4dExWUmxjM1IyWlhKemFXOXVMUzB4Q3pBSkJnTlZCQVlUQWtSRk1SUXdFZ1lEVlFRS0V3dFRkMmx6YzJKcApkQ0JCUnpGSk1FY0dBMVVFQXhOQU1UTTFZakl5TVRWaFpUVTNZVGM0TnpCbFl6RXpZak5rTWpnMVptRTFNak5qCllUQTVOV1ppWkRBeU5URmtNMlEyTkRBMFpXSTRZakkwWXpnellURTRZVEI2TUJRR0J5cUdTTTQ5QWdFR0NTc2sKQXdNQ0NBRUJDd05pQUFSSFJZR25ra0ZJM1pncThHWnZlUFgwZmFvQzF5QzdQSjdhNGNmenBVY05oRHhKaGRmdgpIR1hwL0NneDZpVDVobkUwMVdYSWR3Z1IxTEZWY0VIaXBUNXlxY0NFa2tFRTh4Q2FkM2hGMFJybXBSRHdtR2VFCmJGcFdCaisvRmdOUWlMZWpnZ0UyTUlJQk1qQWZCZ05WSFNNRUdEQVdnQlROdHdaU0VrQlN5clNhK2Q4ajNQSFkKR291Q2VqQWRCZ05WSFE0RUZnUVVJR2VDMTBHbG9hdkRQc1VtMW9NYXpXK2YzU0F3T3dZRFZSMGZCRFF3TWpBdwpvQzZnTElZcWFIUjBjRG92TDJOeWJDNTBjMlV1ZEdWc1pYTmxZeTVrWlM5amNtd3ZWRk5GWDBOQlh6RXVZM0pzCk1BNEdBMVVkRHdFQi93UUVBd0lIZ0RBTUJnTlZIUk1CQWY4RUFqQUFNRTBHQTFVZElBUkdNRVF3UWdZSkt3WUIKQkFHOVJ3MHBNRFV3TXdZSUt3WUJCUVVIQWdFV0oyaDBkSEE2THk5a2IyTnpMblJ6WlM1MFpXeGxjMlZqTG1SbApMMk53Y3k5MGMyVXVhSFJ0YkRCR0JnZ3JCZ0VGQlFjQkFRUTZNRGd3TmdZSUt3WUJCUVVITUFLR0ttaDBkSEE2Ckx5OWpjblF1ZEhObExuUmxiR1Z6WldNdVpHVXZZM0owTDFSVFJWOURRVjh4TG1OeWREQUtCZ2dxaGtqT1BRUUQKQXdObkFEQmtBakFlRFNzcmlTc3MyRVlQUkhqSDhvUnVmTzR4UHBTWE1sMmw3Q1pKQlZYdWFsd1JZZGVia3RLWgphdUwvWlFXTHJSTUNNRWhVRnBEQkdmcUNIYk1UMUM5d3YvaGV4cmF6eGNUN1Fva1M0aDZyTTdUTU93aWJkcGIvCjhKdUIxS3Zjb1pKTGpBPT0KLS0tLS1FTkQgQ0VSVElGSUNBVEUtLS0tLQotLS0tLUJFR0lOIENFUlRJRklDQVRFLS0tLS0KTUlJRFl6Q0NBdXFnQXdJQkFnSVFMQjIvSjJ4d0lxcnVQeUF0ZTlkN2ZqQUtCZ2dxaGtqT1BRUURBekJzTVJzdwpHUVlEVlFRREV4SlVVMFVnVkdWemRDQlNiMjkwSUVOQklERXhKVEFqQmdOVkJBb1RIRlF0VTNsemRHVnRjeUJKCmJuUmxjbTVoZEdsdmJtRnNJRWR0WWtneEdUQVhCZ05WQkFzVEVGUmxiR1ZyYjIwZ1UyVmpkWEpwZEhreEN6QUoKQmdOVkJBWVRBa1JGTUI0WERURTVNVEF3TnpFd05UazBOMW9YRFRNME1UQXdOekl6TlRrMU9Wb3daekVXTUJRRwpBMVVFQXhNTlZGTkZJRlJsYzNRZ1EwRWdNVEVsTUNNR0ExVUVDaE1jVkMxVGVYTjBaVzF6SUVsdWRHVnlibUYwCmFXOXVZV3dnUjIxaVNERVpNQmNHQTFVRUN4TVFWR1ZzWld0dmJTQlRaV04xY21sMGVURUxNQWtHQTFVRUJoTUMKUkVVd2VqQVVCZ2NxaGtqT1BRSUJCZ2tySkFNREFnZ0JBUXNEWWdBRVdBMEtsaURSYzg4TnhvVkNkZWI0V1hwSwpJL1QxT2ZjdGcwUnRuZmlPQXVXUDRIdnJscG50SitLUTl4R0NwUmpnaUZWNHhRUFNDandVUG1pMTF1RXlZQ1FCCnZxeFlwVUNCT2REL1VuUXF0ZmROTVRDNHdEMkJxbHQ1K1ZOamZTaXlvNElCVURDQ0FVd3dId1lEVlIwakJCZ3cKRm9BVUpzaHhxc3l5elpaT1FtclZRYi9VcU9JNVp2MHdSUVlEVlIwZkJENHdQREE2b0RpZ05vWTBhSFIwY0RvdgpMMk55YkM1MGMyVXVkR1ZzWlhObFl5NWtaUzlqY213dlZGTkZYMVJsYzNSZlVtOXZkRjlEUVY4eExtTnliREFkCkJnTlZIUTRFRmdRVXpiY0dVaEpBVXNxMG12bmZJOXp4MkJxTGdub3dEZ1lEVlIwUEFRSC9CQVFEQWdFR01CSUcKQTFVZEV3RUIvd1FJTUFZQkFmOENBUUF3VFFZRFZSMGdCRVl3UkRCQ0Jna3JCZ0VFQWIxSERTa3dOVEF6QmdncgpCZ0VGQlFjQ0FSWW5hSFIwY0RvdkwyUnZZM011ZEhObExuUmxiR1Z6WldNdVpHVXZZM0J6TDNSelpTNW9kRzFzCk1GQUdDQ3NHQVFVRkJ3RUJCRVF3UWpCQUJnZ3JCZ0VGQlFjd0FvWTBhSFIwY0RvdkwyTnlkQzUwYzJVdWRHVnMKWlhObFl5NWtaUzlqY25RdlZGTkZYMVJsYzNSZlVtOXZkRjlEUVY4eExtTmxjakFLQmdncWhrak9QUVFEQXdObgpBREJrQWpBcVNwMG4rMGFJV2FpbXJEZG8wRGtYYlF5TjlBTnN5Z0VQSFZ4VWRKb282VFNvMVErNk16N2dtbG9jCnpmMks5NVVDTUNuTzcyVGYyZ2dkSXJrb0lxS3A3Vm9HQ0JoVlh2ODNTTXFLZi9Tck8xUjBhS2FrcWZkbXJrZEMKNndpVkZieUNFZz09Ci0tLS0tRU5EIENFUlRJRklDQVRFLS0tLS0KLS0tLS1CRUdJTiBDRVJUSUZJQ0FURS0tLS0tCk1JSUNXekNDQWVLZ0F3SUJBZ0lRTFBLU0UrbEExWGE5SjlMR2xGZkUyVEFLQmdncWhrak9QUVFEQXpCc01Sc3cKR1FZRFZRUURFeEpVVTBVZ1ZHVnpkQ0JTYjI5MElFTkJJREV4SlRBakJnTlZCQW9USEZRdFUzbHpkR1Z0Y3lCSgpiblJsY201aGRHbHZibUZzSUVkdFlrZ3hHVEFYQmdOVkJBc1RFRlJsYkdWcmIyMGdVMlZqZFhKcGRIa3hDekFKCkJnTlZCQVlUQWtSRk1CNFhEVEU1TVRBd056RXdORFF4TUZvWERUUTVNVEF3TnpJek5UazFPVm93YkRFYk1Ca0cKQTFVRUF4TVNWRk5GSUZSbGMzUWdVbTl2ZENCRFFTQXhNU1V3SXdZRFZRUUtFeHhVTFZONWMzUmxiWE1nU1c1MApaWEp1WVhScGIyNWhiQ0JIYldKSU1Sa3dGd1lEVlFRTEV4QlVaV3hsYTI5dElGTmxZM1Z5YVhSNU1Rc3dDUVlEClZRUUdFd0pFUlRCNk1CUUdCeXFHU000OUFnRUdDU3NrQXdNQ0NBRUJDd05pQUFRVU9tSmYvSWJDQUg4TTJMaDUKcTZVZVlqWEdVTkUyWHBZd2kyMkVhUTRia1FvQXU5NlMwMmJrdGhRVDJvanUwbEptSm5PaUVyaUlNbWdCMlZBZApSWm55N050RnVLak9NdWJUY2RYUVBkeGlhSWsvQUltQ1ZwdTRqbTV6M0k5WktoV2pSVEJETUIwR0ExVWREZ1FXCkJCUW15SEdxekxMTmxrNUNhdFZCdjlTbzRqbG0vVEFPQmdOVkhROEJBZjhFQkFNQ0FRWXdFZ1lEVlIwVEFRSC8KQkFnd0JnRUIvd0lCQVRBS0JnZ3Foa2pPUFFRREF3Tm5BREJrQWpCL2NpbTFDSk42UEN4ZDE5UHB1TlEzMUdTKwpjdFY2cFIwKzFndW9IVVF3dS83N0hYVFNhTGozRUl1QUNXekYzbHNDTUNpQW9qaXVxMTF2S0lOZ1ZST3FhQ1ZKCmd2ZU1JVlZlWFpXWkVYTVdUUjV4UExFbS9SUGdRNlBJbkdaNDR4WU42UT09Ci0tLS0tRU5EIENFUlRJRklDQVRFLS0tLS0K"
            },
            CurrentClientIds = new List<string>
            {
                  "DN TSEProduction ef82abcedf", "fiskaltrust.Middleware", TseClientId
            },
            CurrentNumberOfClients = 3,
            CurrentStartedTransactionNumbers = new List<ulong>(),
            CurrentState = TseStates.Initialized
        };

        protected override TseInfo ExpectedUninitializedTseInfo => new TseInfo
        {
            CertificationIdentification = "BSI-K-TR-0000-2020",
            PublicKeyBase64 = null,
            SerialNumberOctet = null,
            SignatureAlgorithm = null,
            MaxNumberOfStartedTransactions = 0,
            MaxNumberOfSignatures = 20000000,
            MaxNumberOfClients = 0,
            MaxLogMemorySize = 2751462912,
            LogTimeFormat = null,
            FirmwareIdentification = "0001000200010002",
            CertificatesBase64 = new List<string>(),
            CurrentClientIds = new List<string>(),
            CurrentNumberOfClients = 0,
            CurrentStartedTransactionNumbers = new List<ulong>(),
            CurrentState = TseStates.Uninitialized
        };

        protected override TseInfo ExpectedTermiantedTseInfo => new TseInfo
        {
            CertificationIdentification = "0001000200010002",
            PublicKeyBase64 = null,
            SerialNumberOctet = "47F29FDAC9CC1FB6C8FED230BD6359E2273A5152E6CAC8EEDC49FB532F26660E",
            SignatureAlgorithm = null,
            MaxNumberOfStartedTransactions = 0,
            MaxNumberOfSignatures = 20000000,
            MaxNumberOfClients = 0,
            MaxLogMemorySize = 6979321856,
            LogTimeFormat = null,
            FirmwareIdentification = "Format: SingleTSE; Firmware Version: 1.7.691; Loader Version: 1.1.497",
            CertificatesBase64 = new List<string>
            {
                "LS0tLS1CRUdJTiBDRVJUSUZJQ0FURS0tLS0tCk1JSURaakNDQXUyZ0F3SUJBZ0lRQU4wNW9PT0hDTW1MS1ZJVnhiNEI4ekFLQmdncWhrak9QUVFEQXpCbk1SWXcKRkFZRFZRUURFdzFVVTBVZ1ZHVnpkQ0JEUVNBeE1TVXdJd1lEVlFRS0V4eFVMVk41YzNSbGJYTWdTVzUwWlhKdQpZWFJwYjI1aGJDQkhiV0pJTVJrd0Z3WURWUVFMRXhCVVpXeGxhMjl0SUZObFkzVnlhWFI1TVFzd0NRWURWUVFHCkV3SkVSVEFlRncweU1EQTFNamN5TVRJd05EbGFGdzB5TURFeE1qY3lNelU1TlRsYU1JR0lNUmd3RmdZRFZRUXUKRXc4dExWUmxjM1IyWlhKemFXOXVMUzB4Q3pBSkJnTlZCQVlUQWtSRk1SUXdFZ1lEVlFRS0V3dFRkMmx6YzJKcApkQ0JCUnpGSk1FY0dBMVVFQXhOQU1UTTFZakl5TVRWaFpUVTNZVGM0TnpCbFl6RXpZak5rTWpnMVptRTFNak5qCllUQTVOV1ppWkRBeU5URmtNMlEyTkRBMFpXSTRZakkwWXpnellURTRZVEI2TUJRR0J5cUdTTTQ5QWdFR0NTc2sKQXdNQ0NBRUJDd05pQUFSSFJZR25ra0ZJM1pncThHWnZlUFgwZmFvQzF5QzdQSjdhNGNmenBVY05oRHhKaGRmdgpIR1hwL0NneDZpVDVobkUwMVdYSWR3Z1IxTEZWY0VIaXBUNXlxY0NFa2tFRTh4Q2FkM2hGMFJybXBSRHdtR2VFCmJGcFdCaisvRmdOUWlMZWpnZ0UyTUlJQk1qQWZCZ05WSFNNRUdEQVdnQlROdHdaU0VrQlN5clNhK2Q4ajNQSFkKR291Q2VqQWRCZ05WSFE0RUZnUVVJR2VDMTBHbG9hdkRQc1VtMW9NYXpXK2YzU0F3T3dZRFZSMGZCRFF3TWpBdwpvQzZnTElZcWFIUjBjRG92TDJOeWJDNTBjMlV1ZEdWc1pYTmxZeTVrWlM5amNtd3ZWRk5GWDBOQlh6RXVZM0pzCk1BNEdBMVVkRHdFQi93UUVBd0lIZ0RBTUJnTlZIUk1CQWY4RUFqQUFNRTBHQTFVZElBUkdNRVF3UWdZSkt3WUIKQkFHOVJ3MHBNRFV3TXdZSUt3WUJCUVVIQWdFV0oyaDBkSEE2THk5a2IyTnpMblJ6WlM1MFpXeGxjMlZqTG1SbApMMk53Y3k5MGMyVXVhSFJ0YkRCR0JnZ3JCZ0VGQlFjQkFRUTZNRGd3TmdZSUt3WUJCUVVITUFLR0ttaDBkSEE2Ckx5OWpjblF1ZEhObExuUmxiR1Z6WldNdVpHVXZZM0owTDFSVFJWOURRVjh4TG1OeWREQUtCZ2dxaGtqT1BRUUQKQXdObkFEQmtBakFlRFNzcmlTc3MyRVlQUkhqSDhvUnVmTzR4UHBTWE1sMmw3Q1pKQlZYdWFsd1JZZGVia3RLWgphdUwvWlFXTHJSTUNNRWhVRnBEQkdmcUNIYk1UMUM5d3YvaGV4cmF6eGNUN1Fva1M0aDZyTTdUTU93aWJkcGIvCjhKdUIxS3Zjb1pKTGpBPT0KLS0tLS1FTkQgQ0VSVElGSUNBVEUtLS0tLQotLS0tLUJFR0lOIENFUlRJRklDQVRFLS0tLS0KTUlJRFl6Q0NBdXFnQXdJQkFnSVFMQjIvSjJ4d0lxcnVQeUF0ZTlkN2ZqQUtCZ2dxaGtqT1BRUURBekJzTVJzdwpHUVlEVlFRREV4SlVVMFVnVkdWemRDQlNiMjkwSUVOQklERXhKVEFqQmdOVkJBb1RIRlF0VTNsemRHVnRjeUJKCmJuUmxjbTVoZEdsdmJtRnNJRWR0WWtneEdUQVhCZ05WQkFzVEVGUmxiR1ZyYjIwZ1UyVmpkWEpwZEhreEN6QUoKQmdOVkJBWVRBa1JGTUI0WERURTVNVEF3TnpFd05UazBOMW9YRFRNME1UQXdOekl6TlRrMU9Wb3daekVXTUJRRwpBMVVFQXhNTlZGTkZJRlJsYzNRZ1EwRWdNVEVsTUNNR0ExVUVDaE1jVkMxVGVYTjBaVzF6SUVsdWRHVnlibUYwCmFXOXVZV3dnUjIxaVNERVpNQmNHQTFVRUN4TVFWR1ZzWld0dmJTQlRaV04xY21sMGVURUxNQWtHQTFVRUJoTUMKUkVVd2VqQVVCZ2NxaGtqT1BRSUJCZ2tySkFNREFnZ0JBUXNEWWdBRVdBMEtsaURSYzg4TnhvVkNkZWI0V1hwSwpJL1QxT2ZjdGcwUnRuZmlPQXVXUDRIdnJscG50SitLUTl4R0NwUmpnaUZWNHhRUFNDandVUG1pMTF1RXlZQ1FCCnZxeFlwVUNCT2REL1VuUXF0ZmROTVRDNHdEMkJxbHQ1K1ZOamZTaXlvNElCVURDQ0FVd3dId1lEVlIwakJCZ3cKRm9BVUpzaHhxc3l5elpaT1FtclZRYi9VcU9JNVp2MHdSUVlEVlIwZkJENHdQREE2b0RpZ05vWTBhSFIwY0RvdgpMMk55YkM1MGMyVXVkR1ZzWlhObFl5NWtaUzlqY213dlZGTkZYMVJsYzNSZlVtOXZkRjlEUVY4eExtTnliREFkCkJnTlZIUTRFRmdRVXpiY0dVaEpBVXNxMG12bmZJOXp4MkJxTGdub3dEZ1lEVlIwUEFRSC9CQVFEQWdFR01CSUcKQTFVZEV3RUIvd1FJTUFZQkFmOENBUUF3VFFZRFZSMGdCRVl3UkRCQ0Jna3JCZ0VFQWIxSERTa3dOVEF6QmdncgpCZ0VGQlFjQ0FSWW5hSFIwY0RvdkwyUnZZM011ZEhObExuUmxiR1Z6WldNdVpHVXZZM0J6TDNSelpTNW9kRzFzCk1GQUdDQ3NHQVFVRkJ3RUJCRVF3UWpCQUJnZ3JCZ0VGQlFjd0FvWTBhSFIwY0RvdkwyTnlkQzUwYzJVdWRHVnMKWlhObFl5NWtaUzlqY25RdlZGTkZYMVJsYzNSZlVtOXZkRjlEUVY4eExtTmxjakFLQmdncWhrak9QUVFEQXdObgpBREJrQWpBcVNwMG4rMGFJV2FpbXJEZG8wRGtYYlF5TjlBTnN5Z0VQSFZ4VWRKb282VFNvMVErNk16N2dtbG9jCnpmMks5NVVDTUNuTzcyVGYyZ2dkSXJrb0lxS3A3Vm9HQ0JoVlh2ODNTTXFLZi9Tck8xUjBhS2FrcWZkbXJrZEMKNndpVkZieUNFZz09Ci0tLS0tRU5EIENFUlRJRklDQVRFLS0tLS0KLS0tLS1CRUdJTiBDRVJUSUZJQ0FURS0tLS0tCk1JSUNXekNDQWVLZ0F3SUJBZ0lRTFBLU0UrbEExWGE5SjlMR2xGZkUyVEFLQmdncWhrak9QUVFEQXpCc01Sc3cKR1FZRFZRUURFeEpVVTBVZ1ZHVnpkQ0JTYjI5MElFTkJJREV4SlRBakJnTlZCQW9USEZRdFUzbHpkR1Z0Y3lCSgpiblJsY201aGRHbHZibUZzSUVkdFlrZ3hHVEFYQmdOVkJBc1RFRlJsYkdWcmIyMGdVMlZqZFhKcGRIa3hDekFKCkJnTlZCQVlUQWtSRk1CNFhEVEU1TVRBd056RXdORFF4TUZvWERUUTVNVEF3TnpJek5UazFPVm93YkRFYk1Ca0cKQTFVRUF4TVNWRk5GSUZSbGMzUWdVbTl2ZENCRFFTQXhNU1V3SXdZRFZRUUtFeHhVTFZONWMzUmxiWE1nU1c1MApaWEp1WVhScGIyNWhiQ0JIYldKSU1Sa3dGd1lEVlFRTEV4QlVaV3hsYTI5dElGTmxZM1Z5YVhSNU1Rc3dDUVlEClZRUUdFd0pFUlRCNk1CUUdCeXFHU000OUFnRUdDU3NrQXdNQ0NBRUJDd05pQUFRVU9tSmYvSWJDQUg4TTJMaDUKcTZVZVlqWEdVTkUyWHBZd2kyMkVhUTRia1FvQXU5NlMwMmJrdGhRVDJvanUwbEptSm5PaUVyaUlNbWdCMlZBZApSWm55N050RnVLak9NdWJUY2RYUVBkeGlhSWsvQUltQ1ZwdTRqbTV6M0k5WktoV2pSVEJETUIwR0ExVWREZ1FXCkJCUW15SEdxekxMTmxrNUNhdFZCdjlTbzRqbG0vVEFPQmdOVkhROEJBZjhFQkFNQ0FRWXdFZ1lEVlIwVEFRSC8KQkFnd0JnRUIvd0lCQVRBS0JnZ3Foa2pPUFFRREF3Tm5BREJrQWpCL2NpbTFDSk42UEN4ZDE5UHB1TlEzMUdTKwpjdFY2cFIwKzFndW9IVVF3dS83N0hYVFNhTGozRUl1QUNXekYzbHNDTUNpQW9qaXVxMTF2S0lOZ1ZST3FhQ1ZKCmd2ZU1JVlZlWFpXWkVYTVdUUjV4UExFbS9SUGdRNlBJbkdaNDR4WU42UT09Ci0tLS0tRU5EIENFUlRJRklDQVRFLS0tLS0K"
            },
            CurrentClientIds = new List<string>
            {
                "DN TSEProduction ef82abcedf", "fiskaltrust.Middleware", TseClientId
            },
            CurrentNumberOfClients = 3,
            CurrentStartedTransactionNumbers = new List<ulong>(),
            CurrentState = TseStates.Terminated
        };

        protected override IDESSCD GetResetSystemUnderTest(Dictionary<string, object> configuration = null)
        {
            if (_instance != null && _instance is IDisposable disposable)
            {
                disposable.Dispose();
                _instance = null;
            }

            PerformResetWithSelfTest(_DEVICE_PATH);
            return GetSystemUnderTest(configuration);
        }

        protected override IDESSCD GetSystemUnderTest(Dictionary<string, object> configuration = null)
        {
            if (_instance != null && _instance is IDisposable disposable)
            {
                disposable.Dispose();
                _instance = null;
            }

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            var scuBootStrapper = new DieboldNixdorf.ScuBootstrapper
            {
                Configuration = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(new DieboldNixdorfConfiguration
                {
                    ComPort = _DEVICE_PATH,
                    SlotNumber = 1,
                    AdminUser = "1",
                    AdminPin = "12345"
                }))
            };
            scuBootStrapper.ConfigureServices(serviceCollection);
            _instance = serviceCollection.BuildServiceProvider().GetService<IDESSCD>();
            return _instance;
        }

        public static void PerformResetWithSelfTest(string comPort)
        {
            using (var serialPortCommunicationProvider = new SerialPortCommunicationQueue(Mock.Of<ILogger<SerialPortCommunicationQueue>>(), comPort, 1500, 1500, true))
            {
                var tseCommunicationCommandHelper = new TseCommunicationCommandHelper(Mock.Of<ILogger<TseCommunicationCommandHelper>>(), serialPortCommunicationProvider, 1);
                var authenticationTseCommandProvider = new AuthenticationTseCommandProvider(Mock.Of<ILogger<AuthenticationTseCommandProvider>>(), tseCommunicationCommandHelper);
                var standardTseCommandsProvider = new StandardTseCommandsProvider(tseCommunicationCommandHelper);
                standardTseCommandsProvider.DisableAsb();
                var developerTseCommandProvider = new DeveloperTseCommandsProvider(tseCommunicationCommandHelper);
                developerTseCommandProvider.FactoryReset();
                var adminTseCommandFactory = new MaintenanceTseCommandProvider(tseCommunicationCommandHelper, Mock.Of<ILogger<MaintenanceTseCommandProvider>>());
                authenticationTseCommandProvider.ExecuteAuthorized("1", "12345", () => adminTseCommandFactory.RegisterClient("DN TSEProduction ef82abcedf"));
                var maintenanceTseCommandProvider = new MaintenanceTseCommandProvider(tseCommunicationCommandHelper, Mock.Of<ILogger<MaintenanceTseCommandProvider>>());
                maintenanceTseCommandProvider.RunSelfTest("DN TSEProduction ef82abcedf");
            }
        }

        public static void PerformReset(string comPort)
        {
            using (var serialPortCommunicationProvider = new SerialPortCommunicationQueue(Mock.Of<ILogger<SerialPortCommunicationQueue>>(), comPort, 1500, 1500, true))
            {
                var tseCommunicationCommandHelper = new TseCommunicationCommandHelper(Mock.Of<ILogger<TseCommunicationCommandHelper>>(), serialPortCommunicationProvider, 1);
                var authenticationTseCommandProvider = new AuthenticationTseCommandProvider(Mock.Of<ILogger<AuthenticationTseCommandProvider>>(), tseCommunicationCommandHelper);
                var standardTseCommandsProvider = new StandardTseCommandsProvider(tseCommunicationCommandHelper);
                standardTseCommandsProvider.DisableAsb();
                var developerTseCommandProvider = new DeveloperTseCommandsProvider(tseCommunicationCommandHelper);
                developerTseCommandProvider.FactoryReset();
                var adminTseCommandFactory = new MaintenanceTseCommandProvider(tseCommunicationCommandHelper, Mock.Of<ILogger<MaintenanceTseCommandProvider>>());
                authenticationTseCommandProvider.ExecuteAuthorized("1", "12345", () => adminTseCommandFactory.RegisterClient("DN TSEProduction ef82abcedf"));
            }
        }
    }
}
