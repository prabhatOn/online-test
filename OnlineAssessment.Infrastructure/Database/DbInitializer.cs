using Microsoft.EntityFrameworkCore;
using OnlineAssessment.Infrastructure.Models;
using BC = BCrypt.Net.BCrypt;

namespace OnlineAssessment.Infrastructure.Database
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            // Check if we already have data
            if (context.Users.Any())
            {
                return;
            }

            // Create admin user
            var adminUser = new User
            {
                Username = "admin",
                Email = "admin@onlineassessment.com",
                PasswordHash = BC.HashPassword("Admin@123"),
                FirstName = "Admin",
                LastName = "User",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                IsAdmin = true
            };
            context.Users.Add(adminUser);
            context.SaveChanges();

            // Create sample organization
            var organization = new Organization
            {
                Name = "Sample Organization",
                Description = "A sample organization for testing",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = adminUser.Id
            };
            context.Organizations.Add(organization);
            context.SaveChanges();

            // Create sample test
            var test = new Test
            {
                Title = "Sample Test",
                Description = "A sample test for testing",
                DurationInMinutes = 60,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = adminUser.Id,
                OrganizationId = organization.Id
            };
            context.Tests.Add(test);
            context.SaveChanges();

            // Create sample questions
            var questions = new[]
            {
                new Question
                {
                    TestId = test.Id,
                    Title = "Sample MCQ Question",
                    Description = "What is 2 + 2?",
                    Type = QuestionType.MultipleChoice,
                    Points = 1,
                    CreatedAt = DateTime.UtcNow
                },
                new Question
                {
                    TestId = test.Id,
                    Title = "Sample Coding Question",
                    Description = "Write a function to calculate factorial",
                    Type = QuestionType.Coding,
                    Points = 5,
                    CreatedAt = DateTime.UtcNow
                }
            };
            context.Questions.AddRange(questions);
            context.SaveChanges();

            // Create sample answers for MCQ
            var answers = new[]
            {
                new Answer
                {
                    QuestionId = questions[0].Id,
                    Text = "3",
                    IsCorrect = false,
                    CreatedAt = DateTime.UtcNow
                },
                new Answer
                {
                    QuestionId = questions[0].Id,
                    Text = "4",
                    IsCorrect = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Answer
                {
                    QuestionId = questions[0].Id,
                    Text = "5",
                    IsCorrect = false,
                    CreatedAt = DateTime.UtcNow
                }
            };
            context.Answers.AddRange(answers);
            context.SaveChanges();
        }
    }
} 