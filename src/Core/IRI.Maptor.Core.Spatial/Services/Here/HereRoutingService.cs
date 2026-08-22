using IRI.Maptor.Core.Spatial.Primitives;
using IRI.Maptor.Core.Common.Contracts.Here;
using System;
using System.Globalization;
using System.Threading.Tasks;
using IRI.Maptor.Core.Common.Primitives;
using IRI.Maptor.Core.Common.Services;
using IRI.Maptor.Extensions;

namespace IRI.Maptor.Core.Spatial.Services.Here;

public static class HereRoutingService
{
    public static async Task<Response<HereIsolineResult>> GetIsolineAsync(Point centerGeographic, double timeLimit, string appCode, string appId)
    {
        try
        {
            var pointString = $"{centerGeographic.Y.ToString(CultureInfo.InvariantCulture)},{centerGeographic.X.ToString(CultureInfo.InvariantCulture)}";

            var url = $"https://isoline.route.api.here.com/routing/7.2/calculateisoline.json?app_id={appId}&app_code={appCode}&start=geo!{pointString}&range={timeLimit}&rangetype=time&mode=shortest;car;traffic:enabled";

            return await IRI.Maptor.Core.Common.Helpers.HttpTransport.GetAsync<HereIsolineResult>(url);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);

            return ResponseFactory.CreateError<HereIsolineResult>(ex.GetMessagePlus());
        }
    }
}
