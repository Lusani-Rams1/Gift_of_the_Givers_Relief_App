using Gift_of_the_Givers_Relief_App.Data;
using Gift_of_the_Givers_Relief_App.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Gift_of_the_Givers_Relief_App.Pages.Volunteers
{
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Volunteer? Volunteer { get; private set; }
        public User? Volunteers { get; private set; }
        public List<VolunteerAssignment> Assignments { get; private set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // Get user ID from cookie (same as login sets)
            if (!Request.Cookies.TryGetValue("GgUserId", out var idStr)
                || !int.TryParse(idStr, out var userId))
            {
                return RedirectToPage("/Account/Login");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user is null)
            {
                return RedirectToPage("/Account/Login");
            }

            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(v => v.UserID == userId);

            if (volunteer is null)
            {
                return RedirectToPage("/Index"); // not a volunteer
            }

            if (volunteer.Status != "Approved")
            {
                return RedirectToPage("/Volunteers/Pending");
            }

            Volunteers = user;
            Volunteer = volunteer;
            Assignments = await _context.VolunteerAssignments
                .Where(a => a.VolunteerID == volunteer.VolunteerID)
                .ToListAsync();

            return Page();
        }
    }
}