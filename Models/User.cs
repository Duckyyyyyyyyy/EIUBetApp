using EIUBetApp.Models;
using System.ComponentModel.DataAnnotations;

public class User
{
    [Key]
    public Guid UserId { get; set; }  // NOT NULL

    [StringLength(30)]
    public string? Username { get; set; }

    [Required]
    [StringLength(255)]
    public string Password { get; set; } = string.Empty;  // NOT NULL

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;  // NOT NULL

    [Phone]
    [StringLength(20)]
    public string? Phone { get; set; }  // Nullable in DB

    public bool? IsDeleted { get; set; }  // Nullable in DB

    [StringLength(100)]
    public string? FullName { get; set; }  // Nullable in DB

    //Navigation
    public Player? Player { get; set; }
    public Admin? Admin { get; set; }
    public ICollection<UserRole>? UserRoles { get; set; }
}
