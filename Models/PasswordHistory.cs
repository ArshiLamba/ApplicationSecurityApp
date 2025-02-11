using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

public class PasswordHistory
{
    [Key]
    public int Id { get; set; }
    public int UserId { get; set; }
    public string HashedPassword { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
