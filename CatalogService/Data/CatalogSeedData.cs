using CatalogService.Models.Entities;

namespace CatalogService.Data;

public static class CatalogSeedData
{
    public static async Task SeedAsync(CatalogServiceContext context)
    {
        if (context.Books.Any())
            return;

        var books = new List<Book>
        {
            new()
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-13-468599-1",
                Title = "Clean Code",
                Author = "Robert C. Martin",
                Genre = "Technology",
                PublicationYear = 2008,
                Description = "A handbook of agile software craftsmanship that teaches programmers the principles, patterns, and practices of writing clean code.",
                Publisher = "Prentice Hall",
                PageCount = 431,
                Language = "English",
                TotalCopies = 5,
                AvailableCopies = 2
            },
            new()
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-13-475759-9",
                Title = "Refactoring",
                Author = "Martin Fowler",
                Genre = "Technology",
                PublicationYear = 2018,
                Description = "A guide to improving the design of existing code. The second edition introduces refactoring to a broader audience and covers new refactoring techniques.",
                Publisher = "Addison-Wesley Professional",
                PageCount = 448,
                Language = "English",
                TotalCopies = 3,
                AvailableCopies = 0
            },
            new()
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-13-595705-9",
                Title = "The Pragmatic Programmer",
                Author = "David Thomas, Andrew Hunt",
                Genre = "Technology",
                PublicationYear = 2019,
                Description = "Your journey to mastery. This book helps you examine what it means to be a modern programmer.",
                Publisher = "Addison-Wesley Professional",
                PageCount = 352,
                Language = "English",
                TotalCopies = 4,
                AvailableCopies = 4
            },
            new()
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-20-163361-5",
                Title = "Design Patterns",
                Author = "Gang of Four",
                Genre = "Technology",
                PublicationYear = 1994,
                Description = "Elements of Reusable Object-Oriented Software. The definitive reference for software design patterns.",
                Publisher = "Addison-Wesley Professional",
                PageCount = 395,
                Language = "English",
                TotalCopies = 2,
                AvailableCopies = 0
            },
            new()
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-44-100590-3",
                Title = "Dune",
                Author = "Frank Herbert",
                Genre = "Fiction",
                PublicationYear = 1965,
                Description = "Set on the desert planet Arrakis, Dune is the story of the boy Paul Atreides, heir to a noble family tasked with ruling an inhospitable world.",
                Publisher = "Chilton Books",
                PageCount = 412,
                Language = "English",
                TotalCopies = 10,
                AvailableCopies = 8
            },
            new()
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-45-228423-4",
                Title = "1984",
                Author = "George Orwell",
                Genre = "Fiction",
                PublicationYear = 1949,
                Description = "A dystopian social science fiction novel that follows the life of Winston Smith, a low ranking member of 'the Party' in a totalitarian superstate.",
                Publisher = "Secker & Warburg",
                PageCount = 328,
                Language = "English",
                TotalCopies = 6,
                AvailableCopies = 3
            },
            new()
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-55-305828-7",
                Title = "A Brief History of Time",
                Author = "Stephen Hawking",
                Genre = "Science",
                PublicationYear = 1988,
                Description = "A landmark volume in science writing by one of the great minds of our time, covering topics from the Big Bang to black holes.",
                Publisher = "Bantam Books",
                PageCount = 212,
                Language = "English",
                TotalCopies = 4,
                AvailableCopies = 2
            },
            new()
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-06-231609-7",
                Title = "Sapiens: A Brief History of Humankind",
                Author = "Yuval Noah Harari",
                Genre = "History",
                PublicationYear = 2011,
                Description = "A challenging look at how Homo sapiens came to dominate the world and explores what made us so successful.",
                Publisher = "Harper",
                PageCount = 443,
                Language = "English",
                TotalCopies = 5,
                AvailableCopies = 5
            },
            new()
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-73-521129-2",
                Title = "Atomic Habits",
                Author = "James Clear",
                Genre = "Self-Help",
                PublicationYear = 2018,
                Description = "An Easy and Proven Way to Build Good Habits and Break Bad Ones. A practical guide to forming good habits and breaking bad ones.",
                Publisher = "Avery",
                PageCount = 320,
                Language = "English",
                TotalCopies = 8,
                AvailableCopies = 6
            },
            new()
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-74-325919-4",
                Title = "The Great Gatsby",
                Author = "F. Scott Fitzgerald",
                Genre = "Fiction",
                PublicationYear = 1925,
                Description = "A novel about the American dream set in the Roaring Twenties, following the mysterious millionaire Jay Gatsby and his obsession with Daisy Buchanan.",
                Publisher = "Charles Scribner's Sons",
                PageCount = 180,
                Language = "English",
                TotalCopies = 3,
                AvailableCopies = 1
            },
            new()
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-76-790818-4",
                Title = "A Short History of Nearly Everything",
                Author = "Bill Bryson",
                Genre = "Science",
                PublicationYear = 2003,
                Description = "An overview of the history of science and the universe, written in an approachable, humorous style.",
                Publisher = "Broadway Books",
                PageCount = 544,
                Language = "English",
                TotalCopies = 3,
                AvailableCopies = 0
            },
            new()
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-1-59-030200-5",
                Title = "The Art of War",
                Author = "Sun Tzu",
                Genre = "History",
                PublicationYear = -500,
                Description = "An ancient Chinese military treatise dating from the 5th century BC. Attributed to the ancient Chinese military strategist Sun Tzu.",
                Publisher = "Shambhala Publications",
                PageCount = 273,
                Language = "English",
                TotalCopies = 4,
                AvailableCopies = 4
            },
            new()
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-44-650337-5",
                Title = "Rich Dad Poor Dad",
                Author = "Robert T. Kiyosaki",
                Genre = "Self-Help",
                PublicationYear = 1997,
                Description = "A personal finance book that advocates financial independence and building wealth through investing, real estate, and entrepreneurship.",
                Publisher = "Warner Books",
                PageCount = 336,
                Language = "English",
                TotalCopies = 5,
                AvailableCopies = 3
            },
            new()
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-26-203293-3",
                Title = "Introduction to Algorithms",
                Author = "Thomas H. Cormen, Charles E. Leiserson, Ronald L. Rivest, Clifford Stein",
                Genre = "Technology",
                PublicationYear = 2009,
                Description = "A comprehensive textbook covering a broad range of algorithms in depth. Widely used as a textbook for algorithms courses in universities.",
                Publisher = "MIT Press",
                PageCount = 1292,
                Language = "English",
                TotalCopies = 2,
                AvailableCopies = 1
            },
            new()
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-34-518771-5",
                Title = "The Hitchhiker's Guide to the Galaxy",
                Author = "Douglas Adams",
                Genre = "Fiction",
                PublicationYear = 1979,
                Description = "A comedy science fiction series that follows the misadventures of Arthur Dent, the last surviving man following Earth's demolition.",
                Publisher = "Pan Books",
                PageCount = 193,
                Language = "English",
                TotalCopies = 4,
                AvailableCopies = 4
            }
        };

        await context.Books.AddRangeAsync(books);
        await context.SaveChangesAsync();
    }
}
