using System.ComponentModel.DataAnnotations;

namespace RollblackLegacy.Website.Contracts.Account;

public sealed class RegisterAccountInputModel
{
    [Required(ErrorMessage = "El nombre de cuenta es obligatorio.")]
    [StringLength(24, MinimumLength = 3, ErrorMessage = "El nombre de cuenta debe tener entre 3 y 24 caracteres.")]
    [RegularExpression("^[A-Za-z0-9._-]+$", ErrorMessage = "Solo se permiten letras, numeros, punto, guion y guion bajo.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Introduce un correo valido.")]
    [StringLength(255, ErrorMessage = "El correo es demasiado largo.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contrasena es obligatoria.")]
    [StringLength(64, MinimumLength = 6, ErrorMessage = "La contrasena debe tener entre 6 y 64 caracteres.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma la contrasena.")]
    [Compare(nameof(Password), ErrorMessage = "La confirmacion no coincide.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
