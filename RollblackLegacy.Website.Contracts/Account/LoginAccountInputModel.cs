using System.ComponentModel.DataAnnotations;

namespace RollblackLegacy.Website.Contracts.Account;

public sealed class LoginAccountInputModel
{
    [Required(ErrorMessage = "El nombre de cuenta es obligatorio.")]
    [StringLength(24, MinimumLength = 3, ErrorMessage = "El nombre de cuenta debe tener entre 3 y 24 caracteres.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contrasena es obligatoria.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
