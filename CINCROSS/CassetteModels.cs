namespace CINCROSS;

public class CassetteData
{
	public string Titulo { get; set; } = "";
	public string Imagen { get; set; } = "";
	public string Contenido { get; set; } = "";
	public string Color { get; set; } = "";
	public string PantallaGif { get; set; } = "";
	public string TemaTV { get; set; } = "";
	public string Tags { get; set; } = "";
}

public class FavoriteData
{
	public int CassetteIndex { get; set; }
	public string CassetteTitle { get; set; } = "";
	public string ColorHex { get; set; } = "";
	public string TemaTV { get; set; } = "";
}
