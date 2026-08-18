using CampusHub.BuildingBlocks.Security;
using CampusHub.Catalog.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Catalog.Api.Infrastructure;

public sealed class CatalogSeeder(CatalogDbContext db, ILogger<CatalogSeeder> logger)
{
    private static readonly Guid AlgorithmsId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
    private static readonly Guid LinearId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2");

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await CatalogSchema.EnsureAsync(db, cancellationToken);

        if (!await db.Subjects.AnyAsync(cancellationToken))
        {
            db.Subjects.AddRange(
                new Subject
                {
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"),
                    Code = "CS",
                    Name = "Computer Science",
                    Description = "Algorithms, systems, and software engineering."
                },
                new Subject
                {
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"),
                    Code = "MATH",
                    Name = "Mathematics",
                    Description = "Pure and applied mathematics."
                },
                new Subject
                {
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"),
                    Code = "ENG",
                    Name = "English",
                    Description = "Language, writing, and literature."
                });
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.Courses.AnyAsync(cancellationToken))
        {
            var cs = await db.Subjects.SingleAsync(s => s.Code == "CS", cancellationToken);
            var math = await db.Subjects.SingleAsync(s => s.Code == "MATH", cancellationToken);

            db.Courses.AddRange(
                new Course
                {
                    Id = AlgorithmsId,
                    SubjectId = cs.Id,
                    Title = "Introduction to Algorithms",
                    Subtitle = "Learn analysis, sorting, graphs, and dynamic programming with worked campus examples.",
                    Description = "A practical first course in algorithms. You will analyze running time, implement core data structures, and solve interview-style problems with confidence.",
                    Level = "Beginner",
                    Language = "English",
                    Outcomes = "Analyze time and space complexity with Big-O\nImplement sorting and searching correctly\nModel problems with graphs and BFS/DFS\nSolve classic dynamic programming patterns",
                    Requirements = "Comfortable with loops and arrays in any language\nNo prior algorithms course required",
                    TeacherId = SeedUsers.TeacherId,
                    TeacherName = "Ava Teacher",
                    TeacherEmail = SeedUsers.TeacherEmail,
                    Capacity = 30,
                    RemainingSeats = 30,
                    Price = 49.00m,
                    Status = CourseStatus.Published,
                    CreatedAt = DateTimeOffset.UtcNow,
                    PublishedAt = DateTimeOffset.UtcNow
                },
                new Course
                {
                    Id = LinearId,
                    SubjectId = math.Id,
                    Title = "Linear Algebra",
                    Subtitle = "Vector spaces, matrices, and eigenvalues — built for students who will use them in CS and data.",
                    Description = "From vectors to eigen-decompositions, this course connects the algebra to geometry and to applications you will meet in machine learning and graphics.",
                    Level = "Intermediate",
                    Language = "English",
                    Outcomes = "Compute with matrices and linear systems\nExplain vector spaces, bases, and dimension\nFind eigenvalues and interpret them geometrically\nApply SVD intuition to data problems",
                    Requirements = "High-school algebra and a little trigonometry\nA scientific calculator or notebook is enough",
                    TeacherId = SeedUsers.TeacherId,
                    TeacherName = "Ava Teacher",
                    TeacherEmail = SeedUsers.TeacherEmail,
                    Capacity = 25,
                    RemainingSeats = 25,
                    Price = 39.00m,
                    Status = CourseStatus.Published,
                    CreatedAt = DateTimeOffset.UtcNow,
                    PublishedAt = DateTimeOffset.UtcNow
                },
                new Course
                {
                    Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3"),
                    SubjectId = cs.Id,
                    Title = "Distributed Systems Studio",
                    Subtitle = "Draft studio used to demonstrate teacher-only unpublished content.",
                    Description = "Draft course used to demonstrate teacher-only unpublished content.",
                    Level = "Advanced",
                    Language = "English",
                    TeacherId = SeedUsers.TeacherId,
                    TeacherName = "Ava Teacher",
                    TeacherEmail = SeedUsers.TeacherEmail,
                    Capacity = 15,
                    RemainingSeats = 15,
                    Price = 79.00m,
                    Status = CourseStatus.Draft,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            await db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            await EnrichExistingCoursesAsync(cancellationToken);
        }

        await SeedCurriculumAsync(cancellationToken);
        await SeedCommunityAsync(cancellationToken);
        logger.LogInformation("Catalog seed completed");
    }

    private async Task EnrichExistingCoursesAsync(CancellationToken ct)
    {
        var algorithms = await db.Courses.SingleOrDefaultAsync(c => c.Id == AlgorithmsId, ct);
        if (algorithms is not null && string.IsNullOrWhiteSpace(algorithms.Subtitle))
        {
            algorithms.Subtitle = "Learn analysis, sorting, graphs, and dynamic programming with worked campus examples.";
            algorithms.Level = "Beginner";
            algorithms.Language = "English";
            algorithms.Outcomes = "Analyze time and space complexity with Big-O\nImplement sorting and searching correctly\nModel problems with graphs and BFS/DFS\nSolve classic dynamic programming patterns";
            algorithms.Requirements = "Comfortable with loops and arrays in any language\nNo prior algorithms course required";
        }

        var linear = await db.Courses.SingleOrDefaultAsync(c => c.Id == LinearId, ct);
        if (linear is not null && string.IsNullOrWhiteSpace(linear.Subtitle))
        {
            linear.Subtitle = "Vector spaces, matrices, and eigenvalues — built for students who will use them in CS and data.";
            linear.Level = "Intermediate";
            linear.Language = "English";
            linear.Outcomes = "Compute with matrices and linear systems\nExplain vector spaces, bases, and dimension\nFind eigenvalues and interpret them geometrically\nApply SVD intuition to data problems";
            linear.Requirements = "High-school algebra and a little trigonometry\nA scientific calculator or notebook is enough";
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task SeedCurriculumAsync(CancellationToken ct)
    {
        if (await db.CourseSections.AnyAsync(ct))
        {
            return;
        }

        AddSection(AlgorithmsId, 1, "Getting started",
            Lecture(1, "Welcome to the course", "Video", 4, true,
                "How the course is structured and how to use the player, Q&A, and reviews.",
                "Welcome. Each section has a short lecture plus a worked example.\n\nUse Preview lectures before you enroll. After enrollment, the full article body unlocks and you can post reviews and questions.\n\nSuggested pace: two lectures a sitting, then try the practice prompt at the end of each article."),
            Lecture(2, "What is an algorithm?", "Article", 12, true,
                "A first definition, plus why analysis matters more than clever tricks.",
                "An algorithm is a finite, unambiguous procedure that transforms input into output.\n\nWe care about three properties: correctness, efficiency, and simplicity. A slow correct algorithm is still useful as a baseline. A fast incorrect algorithm is a bug.\n\nIn this course we write algorithms in plain language first, then in code-shaped steps. That habit is what interviewers and code reviews actually reward.\n\nPractice: write the steps to find the maximum value in an unsorted list of n numbers. How many comparisons do you need?"));
        AddSection(AlgorithmsId, 2, "Asymptotic analysis",
            Lecture(1, "Big-O, Theta, and Omega", "Article", 18, false,
                "Read running time the way a compiler and a hiring panel do.",
                "Big-O is an upper bound. Theta is a tight bound. Omega is a lower bound.\n\nWe drop constants and lower-order terms because they stop mattering as n grows. 3n² + 12n + 7 is O(n²).\n\nCommon families you should recognize on sight: O(1), O(log n), O(n), O(n log n), O(n²), O(2ⁿ).\n\nWorked example: nested loops over an n × n matrix are O(n²). A binary search over a sorted array is O(log n).\n\nPractice: classify the running time of merge sort's merge step on two lists of length n/2."),
            Lecture(2, "Best, worst, and average case", "Article", 14, false,
                "Why quicksort's average is not the same as its worst case.",
                "Worst case is the guarantee. Average case needs a model of the input. Best case is rarely the number you quote.\n\nLinear search is O(1) best and O(n) worst. Quicksort is O(n log n) average and O(n²) if the pivot is always extreme.\n\nWhen you choose a data structure, ask which case your product actually hits. A campus waitlist that is almost always empty is not the same as a full registration queue."));
        AddSection(AlgorithmsId, 3, "Sorting and graphs",
            Lecture(1, "Merge sort vs quicksort", "Video", 16, false,
                "Stable vs in-place, and when to pick which.",
                "Merge sort is stable and O(n log n) always. It needs extra memory. Quicksort is often faster in practice and can be in-place, but the worst case is quadratic unless you randomize or median-of-three the pivot.\n\nUse merge sort (or a library timsort) when stability matters — for example sorting students by grade, then keeping their original order for equal grades.\n\nPractice: sort 8 integers with merge sort on paper and count the merges."),
            Lecture(2, "BFS and DFS on campus maps", "Article", 20, false,
                "Traverse graphs with a queue or a stack.",
                "Represent buildings as nodes and walkways as edges. BFS finds the fewest hops. DFS explores a path fully before backtracking.\n\nBFS uses a queue. Mark a node visited when you enqueue it, not when you dequeue it, or you will explode the queue on dense graphs.\n\nDFS can be recursive or an explicit stack. Watch the recursion depth on long corridors.\n\nPractice: from the library, list the BFS order of neighboring buildings if each edge has the same walking time."));
        AddSection(AlgorithmsId, 4, "Dynamic programming",
            Lecture(1, "The DP checklist", "Article", 22, false,
                "Optimal substructure, overlapping subproblems, and a table.",
                "Dynamic programming is recursion plus a cache — or a table filled in an order that makes the cache unnecessary.\n\nChecklist: (1) define the state, (2) write the recurrence, (3) identify the base cases, (4) decide top-down or bottom-up, (5) recover the answer.\n\nClassic starters: Fibonacci, coin change, longest common subsequence, 0/1 knapsack.\n\nPractice: write the recurrence for the number of ways to climb n stairs taking 1 or 2 steps at a time."));

        AddSection(LinearId, 1, "Vectors and spaces",
            Lecture(1, "Vectors you can see", "Video", 8, true,
                "Arrows, coordinates, and why two numbers can mean a point or a direction.",
                "A vector is both a point from the origin and a displacement. Adding vectors is walking one path then another. Scaling stretches or flips it.\n\nIn R² we write columns. The standard basis i and j let you rebuild any vector as a combination.\n\nPractice: draw u = (2, 1) and v = (−1, 2). Sketch u+v and 2u − v."),
            Lecture(2, "Linear combinations and span", "Article", 15, false,
                "What it means for vectors to fill a plane.",
                "The span of a set of vectors is every linear combination you can form. Two non-parallel vectors in R² span the plane. Parallel vectors span only a line.\n\nA set is linearly independent when the only combination that yields zero is all coefficients zero.\n\nThis is the language of bases: a basis is an independent spanning set."));
        AddSection(LinearId, 2, "Matrices and systems",
            Lecture(1, "Row reduction that stays honest", "Article", 18, false,
                "Gaussian elimination without losing the original question.",
                "A matrix is a linear map. Solving Ax = b asks whether b lives in the column space of A.\n\nRow reduction produces an equivalent system. Pivot columns form a basis for the column space. Free variables parametrize the null space.\n\nPractice: row-reduce [[1, 2, 3], [2, 4, 8]] and describe the solution set."),
            Lecture(2, "Inverses and what they cannot do", "Article", 12, false,
                "Only square full-rank maps reverse uniquely.",
                "A is invertible iff its columns are independent iff det(A) ≠ 0 iff 0 is not an eigenvalue.\n\nNever invert a matrix just to solve Ax = b — factor or reduce instead. Inverses are a concept; factorization is an algorithm."));
        AddSection(LinearId, 3, "Eigenvalues and applications",
            Lecture(1, "Eigenvectors as invariant directions", "Video", 17, false,
                "The map stretches some arrows and only those arrows keep their line.",
                "Av = λv. The vector stays on its line; the scalar λ says stretch or flip.\n\nCharacteristic polynomial det(A − λI) = 0. Multiplicity can be algebraic or geometric — they need not match, which is why not every matrix diagonalizes.\n\nPractice: find eigenvalues of [[2, 1], [0, 3]]."),
            Lecture(2, "A first look at SVD", "Article", 16, false,
                "Why data people keep saying “singular values”.",
                "Any matrix factors as UΣVᵀ. Singular values are the stretch amounts. Large singular values are the important directions in a dataset.\n\nYou do not need the full proof in this course. You do need the picture: rotation, stretch, rotation. That picture is principal component analysis in disguise."));

        await db.SaveChangesAsync(ct);
    }

    private void AddSection(Guid courseId, int order, string title, params Lecture[] lectures)
    {
        var section = new CourseSection
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            Title = title,
            SortOrder = order
        };
        foreach (var lecture in lectures)
        {
            lecture.SectionId = section.Id;
            section.Lectures.Add(lecture);
        }

        db.CourseSections.Add(section);
    }

    private static Lecture Lecture(int order, string title, string kind, int minutes, bool preview, string summary, string body) =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Kind = kind,
            DurationMinutes = minutes,
            IsPreview = preview,
            SortOrder = order,
            Summary = summary,
            Body = body
        };

    private async Task SeedCommunityAsync(CancellationToken ct)
    {
        if (!await db.CourseReviews.AnyAsync(ct))
        {
            db.CourseReviews.AddRange(
                new CourseReview
                {
                    Id = Guid.NewGuid(),
                    CourseId = AlgorithmsId,
                    StudentId = SeedUsers.StudentId,
                    StudentName = "Sam Student",
                    Rating = 5,
                    Title = "Clear and practical",
                    Body = "The Big-O lectures finally made worst vs average case click. Preview lessons are honest about what you get after enrolling.",
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-6)
                },
                new CourseReview
                {
                    Id = Guid.NewGuid(),
                    CourseId = AlgorithmsId,
                    StudentId = "reviewer-jordan",
                    StudentName = "Jordan Lee",
                    Rating = 4,
                    Title = "Great graphs section",
                    Body = "BFS on the campus map example is the kind of intuition I wish every algorithms class used. Wanted one more coding exercise.",
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-3)
                },
                new CourseReview
                {
                    Id = Guid.NewGuid(),
                    CourseId = LinearId,
                    StudentId = "reviewer-mina",
                    StudentName = "Mina Patel",
                    Rating = 5,
                    Title = "Finally not just formulas",
                    Body = "Eigenvectors as invariant directions is the explanation I will remember. Pacing is fair for an intermediate course.",
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-4)
                });
        }

        if (!await db.CourseQuestions.AnyAsync(ct))
        {
            var question = new CourseQuestion
            {
                Id = Guid.NewGuid(),
                CourseId = AlgorithmsId,
                AuthorId = SeedUsers.StudentId,
                AuthorName = "Sam Student",
                Title = "When is quicksort the wrong default?",
                Body = "If a library already has a stable sort, should I still reach for quicksort in an interview answer?",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-2)
            };
            question.Answers.Add(new CourseAnswer
            {
                Id = Guid.NewGuid(),
                AuthorId = SeedUsers.TeacherId,
                AuthorName = "Ava Teacher",
                Body = "Name the constraints first. If stability or a worst-case guarantee matters, say merge sort or heapsort. Quicksort is a strong average-case default only when you also mention randomized pivots.",
                IsTeacher = true,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-2).AddHours(3)
            });
            db.CourseQuestions.Add(question);
        }

        await db.SaveChangesAsync(ct);
    }
}
