using System.ComponentModel.DataAnnotations;

public class UpdateProfileViewModel
{
    [Required]
    public Guid PlayerId { get; set; }

    [Required]
    [StringLength(30)]
    public string Username { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }
}
