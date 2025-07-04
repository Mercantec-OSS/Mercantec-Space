﻿using Backend.Data;
using Backend.DiscordServices.Services;
using Backend.Models;
using Backend.Models.DTOs;
using Discord;
using Microsoft.EntityFrameworkCore;

namespace Backend.DBAccess
{
    public class DiscordBotDBAccess
    {
        private readonly ApplicationDbContext _context;

        public DiscordBotDBAccess(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetUser(string discordId)
        {
            // Tjek om Discord ID allerede er verificeret
            var user = await _context.Users.FirstOrDefaultAsync(u => u.DiscordId == discordId);
            return user;
        }

        public async Task<UserDailyActivity?> CheckTodaysActivity(string userId, string activityName, DateTime today)
        {
            var dailyActivity = await _context.Set<UserDailyActivity>()
            .FirstOrDefaultAsync(a => a.UserId == userId &&
                                     a.ActivityType == activityName &&
                                     a.Date == today);
            return dailyActivity;
        }

        public async Task<UserDailyActivity?> CheckIfDailyLoginXPIsRewarded(string userId, DateTime today)
        {
            var dailyLoginActivity = await _context.Set<UserDailyActivity>()
            .FirstOrDefaultAsync(a => a.UserId == userId &&
                                    a.ActivityType == XPActivityType.DailyLogin.ToString() &&
                                    a.Date == today);
            return dailyLoginActivity;
        }

        public async Task AddDailyActivity(UserDailyActivity dailyActivity)
        {
            _context.Set<UserDailyActivity>().Add(dailyActivity);

            await _context.SaveChangesAsync();
        }

        public async Task UpdateDailyAcitivity(UserDailyActivity dailyActivity)
        {
            _context.Entry(dailyActivity).State = EntityState.Modified;

            await _context.SaveChangesAsync();
        }

        public async Task<List<UserDailyActivity>> GetAllTodaysActivity(string userId, DateTime today)
        {
            var dailyActivities = await _context.Set<UserDailyActivity>()
                .Where(a => a.UserId == userId && a.Date == today)
                .ToListAsync();

            return dailyActivities;
        }

        public async Task UpdateUser(User user)
        {
            _context.Entry(user).State = EntityState.Modified;

            await _context.SaveChangesAsync();
        }

        public async Task AddUser(User newUser)
        {
            _context.Users.Add(newUser);

            await _context.SaveChangesAsync();
        }

        public async Task<List<User>> GetTopUsers(int amount = 5)
        {
            var topUsers = await _context
                    .Users.Where(u => u.IsBot == null || u.IsBot == false) // Ændret fra GetValueOrDefault
                    .OrderByDescending(u => u.Experience)
                    .Take(amount)
                    .ToListAsync();

            return topUsers;
        }

        public async Task<int> GetUserPosition(int userExperience)
        {
            var userPosition = await _context
                    .Users.Where(u =>
                        (u.IsBot == null || u.IsBot == false) && u.Experience >= userExperience
                    )
                    .CountAsync();

            return userPosition;
        }
    }
}
