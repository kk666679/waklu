// HalalChain.Web/Infrastructure/_Compatibility.cs
// Compatibility shim: re-exports shared primitives from HalalChain.Platform.Http
// so existing Web code compiles unchanged during transition.
// Remove this file in a follow-up PR once Web code updates its using directives.

namespace HalalChain.Web.Infrastructure;

using IApiClient = HalalChain.Platform.Http.Abstractions.IApiClient;
using IFeatureFlag = HalalChain.Platform.Http.Abstractions.IFeatureFlag;
using IApiNotifier = HalalChain.Platform.Http.Abstractions.IApiNotifier;
using ICurrentUserAccessor = HalalChain.Platform.Http.Abstractions.ICurrentUserAccessor;
using Result = HalalChain.Platform.Http.Models.Result;
using Result_T = HalalChain.Platform.Http.Models.Result<object>;
using Error = HalalChain.Platform.Http.Models.Error;
using ErrorKind = HalalChain.Platform.Http.Models.ErrorKind;
using FeatureFlagOptions = HalalChain.Platform.Http.Flags.FeatureFlagOptions;
using FlagNames = HalalChain.Platform.Http.Flags.FlagNames;
using ConfigFeatureFlag = HalalChain.Platform.Http.Flags.ConfigFeatureFlag;
using CorrelationIdHandler = HalalChain.Platform.Http.Resilience.CorrelationIdHandler;
using ApiErrorMapper = HalalChain.Platform.Http.Resilience.ApiErrorMapper;