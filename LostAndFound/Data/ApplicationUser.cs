using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using LostAndFound.Models;

namespace LostAndFound.Data;
// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    public ICollection<Report> Reports { get; set; } = new List<Report>();

    public ICollection<ItemClaim> ItemClaims { get; set; } = new List<ItemClaim>();
}
