using MotorcycleDealer.Mobile.Core.Contracts;
using MotorcycleDealer.Mobile.Core.Security;

namespace MotorcycleDealer.Mobile.Core.Media;

/// <summary>
/// Deterministic offline-first motorcycle studio &amp; editorial hero image resolver.
/// Resolves seeded showroom models to bundled MAUI resource filenames at 0ms with zero network traffic,
/// and resolves Admin-created custom relative/HTTPS URLs cleanly with safe showroom hero fallback.
/// </summary>
public sealed record MotorcycleGalleryAngle(
    string AngleKey,
    string ImageSource,
    string LabelResourceKey);

public interface IMotorcycleImageResolver
{
    string ResolveHeroShowroom();
    string ResolveHeroWorkshop();
    string Resolve(string? modelCode, string? imageUrl = null);
    string ResolveForModel(MotorcycleModelDto? model);
    string ResolveForVehicle(CustomerVehicleDto? vehicle);
    string ResolveForInventory(DealerInventoryDto? inventory);
    IReadOnlyList<MotorcycleGalleryAngle> ResolveGalleryForModel(MotorcycleModelDto? model);
}

public sealed class MotorcycleImageResolver : IMotorcycleImageResolver
{
    public const string HeroShowroomImage = "hero_showroom_apex.jpg";
    public const string HeroWorkshopImage = "hero_workshop_bay.jpg";

    public const string ModelHondaAirblade160 = "model_honda_airblade_160.jpg";
    public const string ModelHondaAirblade160Side = "model_honda_airblade_160_side.jpg";
    public const string ModelHondaAirblade160Rear = "model_honda_airblade_160_rear.jpg";
    public const string ModelHondaAirblade160Cockpit = "model_honda_airblade_160_cockpit.jpg";

    public const string ModelHondaSh160iSport = "model_honda_sh160i_sport.jpg";
    public const string ModelHondaSh160iSportSide = "model_honda_sh160i_sport_side.jpg";
    public const string ModelHondaSh160iSportRear = "model_honda_sh160i_sport_rear.jpg";
    public const string ModelHondaSh160iSportCockpit = "model_honda_sh160i_sport_cockpit.jpg";

    public const string ModelHondaWinnerX = "model_honda_winner_x.jpg";
    public const string ModelHondaWinnerXSide = "model_honda_winner_x_side.jpg";
    public const string ModelHondaWinnerXRear = "model_honda_winner_x_rear.jpg";
    public const string ModelHondaWinnerXCockpit = "model_honda_winner_x_cockpit.jpg";

    public const string ModelYamahaExciter155 = "model_yamaha_exciter_155.jpg";
    public const string ModelYamahaExciter155Side = "model_yamaha_exciter_155_side.jpg";
    public const string ModelYamahaExciter155Rear = "model_yamaha_exciter_155_rear.jpg";
    public const string ModelYamahaExciter155Cockpit = "model_yamaha_exciter_155_cockpit.jpg";

    public const string ModelHondaCb300r = "model_honda_cb300r.jpg";
    public const string ModelHondaCb300rSide = "model_honda_cb300r_side.jpg";
    public const string ModelHondaCb300rRear = "model_honda_cb300r_rear.jpg";
    public const string ModelHondaCb300rCockpit = "model_honda_cb300r_cockpit.jpg";

    public const string ModelHondaNx500 = "model_honda_nx500.jpg";
    public const string ModelHondaNx500Side = "model_honda_nx500_side.jpg";
    public const string ModelHondaNx500Rear = "model_honda_nx500_rear.jpg";
    public const string ModelHondaNx500Cockpit = "model_honda_nx500_cockpit.jpg";

    private static readonly Dictionary<string, IReadOnlyList<MotorcycleGalleryAngle>> BundledGalleriesByPrimary =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [ModelHondaAirblade160] =
            [
                new("front", ModelHondaAirblade160, "ShowroomAngleFront"),
                new("side", ModelHondaAirblade160Side, "ShowroomAngleSide"),
                new("rear", ModelHondaAirblade160Rear, "ShowroomAngleRear"),
                new("cockpit", ModelHondaAirblade160Cockpit, "ShowroomAngleCockpit")
            ],
            [ModelHondaSh160iSport] =
            [
                new("front", ModelHondaSh160iSport, "ShowroomAngleFront"),
                new("side", ModelHondaSh160iSportSide, "ShowroomAngleSide"),
                new("rear", ModelHondaSh160iSportRear, "ShowroomAngleRear"),
                new("cockpit", ModelHondaSh160iSportCockpit, "ShowroomAngleCockpit")
            ],
            [ModelHondaWinnerX] =
            [
                new("front", ModelHondaWinnerX, "ShowroomAngleFront"),
                new("side", ModelHondaWinnerXSide, "ShowroomAngleSide"),
                new("rear", ModelHondaWinnerXRear, "ShowroomAngleRear"),
                new("cockpit", ModelHondaWinnerXCockpit, "ShowroomAngleCockpit")
            ],
            [ModelYamahaExciter155] =
            [
                new("front", ModelYamahaExciter155, "ShowroomAngleFront"),
                new("side", ModelYamahaExciter155Side, "ShowroomAngleSide"),
                new("rear", ModelYamahaExciter155Rear, "ShowroomAngleRear"),
                new("cockpit", ModelYamahaExciter155Cockpit, "ShowroomAngleCockpit")
            ],
            [ModelHondaCb300r] =
            [
                new("front", ModelHondaCb300r, "ShowroomAngleFront"),
                new("side", ModelHondaCb300rSide, "ShowroomAngleSide"),
                new("rear", ModelHondaCb300rRear, "ShowroomAngleRear"),
                new("cockpit", ModelHondaCb300rCockpit, "ShowroomAngleCockpit")
            ],
            [ModelHondaNx500] =
            [
                new("front", ModelHondaNx500, "ShowroomAngleFront"),
                new("side", ModelHondaNx500Side, "ShowroomAngleSide"),
                new("rear", ModelHondaNx500Rear, "ShowroomAngleRear"),
                new("cockpit", ModelHondaNx500Cockpit, "ShowroomAngleCockpit")
            ]
        };

    private static readonly Dictionary<string, string> ModelCodeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["HND-AB160-2026"] = ModelHondaAirblade160,
        ["HND-SH160I-2026"] = ModelHondaSh160iSport,
        ["HND-WINX-2026"] = ModelHondaWinnerX,
        ["YMH-EXC155-2026"] = ModelYamahaExciter155,
        ["HND-CB300R-2026"] = ModelHondaCb300r,
        ["HND-NX500-2026"] = ModelHondaNx500
    };

    private static readonly Dictionary<string, string> SlugMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["honda-airblade-160"] = ModelHondaAirblade160,
        ["airblade-160"] = ModelHondaAirblade160,
        ["model_honda_airblade_160"] = ModelHondaAirblade160,
        ["honda-sh160i-sport"] = ModelHondaSh160iSport,
        ["sh160i-sport"] = ModelHondaSh160iSport,
        ["sh160i"] = ModelHondaSh160iSport,
        ["model_honda_sh160i_sport"] = ModelHondaSh160iSport,
        ["honda-winner-x"] = ModelHondaWinnerX,
        ["winner-x"] = ModelHondaWinnerX,
        ["model_honda_winner_x"] = ModelHondaWinnerX,
        ["yamaha-exciter-155"] = ModelYamahaExciter155,
        ["exciter-155"] = ModelYamahaExciter155,
        ["model_yamaha_exciter_155"] = ModelYamahaExciter155,
        ["honda-cb300r"] = ModelHondaCb300r,
        ["cb300r"] = ModelHondaCb300r,
        ["model_honda_cb300r"] = ModelHondaCb300r,
        ["honda-nx500"] = ModelHondaNx500,
        ["nx500"] = ModelHondaNx500,
        ["model_honda_nx500"] = ModelHondaNx500,
        ["showroom-hero-apex"] = HeroShowroomImage,
        ["hero_showroom_apex"] = HeroShowroomImage,
        ["workshop-bay-apex"] = HeroWorkshopImage,
        ["hero_workshop_bay"] = HeroWorkshopImage
    };

    private static readonly HashSet<string> BundledResourceFilenames = new(StringComparer.OrdinalIgnoreCase)
    {
        ModelHondaAirblade160, ModelHondaAirblade160Side, ModelHondaAirblade160Rear, ModelHondaAirblade160Cockpit,
        ModelHondaSh160iSport, ModelHondaSh160iSportSide, ModelHondaSh160iSportRear, ModelHondaSh160iSportCockpit,
        ModelHondaWinnerX, ModelHondaWinnerXSide, ModelHondaWinnerXRear, ModelHondaWinnerXCockpit,
        ModelYamahaExciter155, ModelYamahaExciter155Side, ModelYamahaExciter155Rear, ModelYamahaExciter155Cockpit,
        ModelHondaCb300r, ModelHondaCb300rSide, ModelHondaCb300rRear, ModelHondaCb300rCockpit,
        ModelHondaNx500, ModelHondaNx500Side, ModelHondaNx500Rear, ModelHondaNx500Cockpit,
        HeroShowroomImage,
        HeroWorkshopImage
    };

    private static IApiEndpointProvider? _ambientEndpointProvider;
    private static MotorcycleImageResolver _current = new(null);

    private readonly IApiEndpointProvider? _endpointProvider;

    public MotorcycleImageResolver(IApiEndpointProvider? endpointProvider = null)
    {
        _endpointProvider = endpointProvider;
        if (endpointProvider is not null)
        {
            _ambientEndpointProvider = endpointProvider;
            _current = this;
        }
    }

    public static MotorcycleImageResolver Current => _current;

    public string ResolveHeroShowroom() => HeroShowroomImage;

    public string ResolveHeroWorkshop() => HeroWorkshopImage;

    public string Resolve(string? modelCode, string? imageUrl = null) =>
        ResolveModelImage(modelCode, imageUrl, _endpointProvider ?? _ambientEndpointProvider);

    public string ResolveForModel(MotorcycleModelDto? model) =>
        model is null ? HeroShowroomImage : Resolve(model.ModelCode, model.ImageUrl);

    public string ResolveForVehicle(CustomerVehicleDto? vehicle) =>
        vehicle is null ? HeroShowroomImage : Resolve(vehicle.ModelCode, vehicle.ImageUrl);

    public string ResolveForInventory(DealerInventoryDto? inventory) =>
        inventory is null ? HeroShowroomImage : Resolve(inventory.ModelCode, null);

    public IReadOnlyList<MotorcycleGalleryAngle> ResolveGalleryForModel(MotorcycleModelDto? model) =>
        ResolveModelGallery(model?.ModelCode, model?.ImageUrl, _endpointProvider ?? _ambientEndpointProvider);

    public static IReadOnlyList<MotorcycleGalleryAngle> ResolveModelGallery(
        string? modelCode,
        string? imageUrl = null,
        IApiEndpointProvider? endpointProvider = null)
    {
        var primary = ResolveModelImage(modelCode, imageUrl, endpointProvider);
        if (BundledGalleriesByPrimary.TryGetValue(primary, out var gallery))
        {
            return gallery;
        }

        return [new MotorcycleGalleryAngle("front", primary, "ShowroomAngleFront")];
    }

    /// <summary>
    /// Deterministic resolution order:
    /// 1. Seeded canonical ModelCode (e.g. HND-SH160I-2026) -> bundled MAUI resource (0ms offline).
    /// 2. Seeded ImageUrl path or bundled filename (e.g. /assets/models/honda-sh160i-sport.jpg) -> bundled MAUI resource.
    /// 3. Absolute https:// (or local dev http://) custom Admin ImageUrl -> preserved URL.
    /// 4. Relative custom Admin /assets/... path -> resolved via IApiEndpointProvider.
    /// 5. Safe bundled fallback -> hero_showroom_apex.jpg.
    /// </summary>
    public static string ResolveModelImage(
        string? modelCode,
        string? imageUrl = null,
        IApiEndpointProvider? endpointProvider = null)
    {
        // 1. Direct canonical ModelCode match (zero-network offline-first)
        if (!string.IsNullOrWhiteSpace(modelCode))
        {
            var trimmedCode = modelCode.Trim();
            if (ModelCodeMap.TryGetValue(trimmedCode, out var mappedByCode))
            {
                return mappedByCode;
            }
        }

        // 2. Seeded ImageUrl slug or bundled resource filename match
        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            var trimmedUrl = imageUrl.Trim();
            if (BundledResourceFilenames.Contains(trimmedUrl))
            {
                return trimmedUrl.ToLowerInvariant();
            }

            var slug = ExtractFileStem(trimmedUrl);
            if (!string.IsNullOrWhiteSpace(slug) && SlugMap.TryGetValue(slug, out var mappedBySlug))
            {
                return mappedBySlug;
            }

            // 3. Absolute HTTPS (or local dev HTTP) custom Admin URL
            if (Uri.TryCreate(trimmedUrl, UriKind.Absolute, out var absoluteUri))
            {
                if (string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                    (string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                     IsAllowedLocalCleartextHost(absoluteUri.Host)))
                {
                    return absoluteUri.AbsoluteUri;
                }

                return HeroShowroomImage;
            }

            // 4. Relative custom Admin asset path (/assets/...) -> combine with active API base URL
            var activeProvider = endpointProvider ?? _ambientEndpointProvider;
            if (activeProvider is not null &&
                (trimmedUrl.StartsWith("/", StringComparison.Ordinal) ||
                 trimmedUrl.StartsWith("assets/", StringComparison.OrdinalIgnoreCase)))
            {
                return activeProvider.BuildApiUri(trimmedUrl).AbsoluteUri;
            }
        }

        // 5. Fallback heuristic on ModelCode fragments (e.g., future year variants HND-SH160I-2027)
        if (!string.IsNullOrWhiteSpace(modelCode))
        {
            var upperCode = modelCode.Trim().ToUpperInvariant();
            if (upperCode.Contains("AB160", StringComparison.Ordinal) || upperCode.Contains("AIRBLADE", StringComparison.Ordinal))
            {
                return ModelHondaAirblade160;
            }
            if (upperCode.Contains("SH160", StringComparison.Ordinal))
            {
                return ModelHondaSh160iSport;
            }
            if (upperCode.Contains("WINX", StringComparison.Ordinal) || upperCode.Contains("WINNER", StringComparison.Ordinal))
            {
                return ModelHondaWinnerX;
            }
            if (upperCode.Contains("EXC155", StringComparison.Ordinal) || upperCode.Contains("EXCITER", StringComparison.Ordinal))
            {
                return ModelYamahaExciter155;
            }
            if (upperCode.Contains("CB300", StringComparison.Ordinal))
            {
                return ModelHondaCb300r;
            }
            if (upperCode.Contains("NX500", StringComparison.Ordinal))
            {
                return ModelHondaNx500;
            }
        }

        return HeroShowroomImage;
    }

    private static string ExtractFileStem(string rawPath)
    {
        var clean = rawPath;
        var queryIdx = clean.IndexOfAny(['?', '#']);
        if (queryIdx >= 0)
        {
            clean = clean[..queryIdx];
        }

        var slashIdx = clean.LastIndexOf('/');
        var fileName = slashIdx >= 0 ? clean[(slashIdx + 1)..] : clean;
        var dotIdx = fileName.LastIndexOf('.');
        return dotIdx > 0 ? fileName[..dotIdx] : fileName;
    }

    private static bool IsAllowedLocalCleartextHost(string host) =>
        string.Equals(host, "10.0.2.2", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase);
}
