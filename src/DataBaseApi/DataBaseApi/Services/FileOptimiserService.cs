using ImageMagick;

namespace DataBaseApi.Services
{
    public class FileOptimiserService
    {
        public static async Task<byte[]> ConvertFileToAvifAsync(IFormFile file)
        {
            await using var inputStream = file.OpenReadStream();

            using var image = new MagickImage(inputStream);
            image.Format = MagickFormat.Avif;
            image.Quality = 50;

            using var outputStream = new MemoryStream();
            image.Write(outputStream);

            return outputStream.ToArray();
        }
    }
}
