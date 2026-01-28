
public record IdDto(int id_pictures);
public record UpdateVisibilityDto(int id_rooms, bool Hidden);
public record UpdateVisibilityTourDto(int id_tours, bool Hidden);
public record NameDto(string Name);
public record UpdateBuildingDto(int IdBuildings, string Name);
public record InsertLinkDto(int id_pictures, string PosX, string PosY, string PosZ, int id_pictures_destination);
public record UpdateLinkDto(int id_links, int id_pictures, string PosX, string PosY, string PosZ, int id_pictures_destination);
public record UpdateTourStepsDto(int id_tours, IEnumerable<dynamic> Steps, string? Title, string? Description);
public record AddTourStepDto(int id_tours, dynamic Step);
public record CreateTourDto(string Title, string Description, List<StepDto> Steps);
public record AddTourDto(string Title, string Description, IEnumerable<dynamic> Steps);
public record InsertInfoPopUpDto(int IdPictures, double PosX, double PosY, double PosZ, string Text, string Title);
public class StepDto
{
    public string? id_tour_steps { get; set; }
    public int id_rooms { get; set; }
    public int step_number { get; set; }
}

public class FileDto { public IFormFile file { get; set; } }

public record UpdateTranslationDto(int id_translation, string text);
public record InsertTranslationDto(int id_language,string translation_namespace,string translation_key,string text);

public record CreateLanguageDto(string name_language);

public record UpdateLanguageDto(string name_language, int id_language);