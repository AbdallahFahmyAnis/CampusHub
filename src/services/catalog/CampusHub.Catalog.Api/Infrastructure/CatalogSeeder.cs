using CampusHub.BuildingBlocks.Security;
using CampusHub.Catalog.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Catalog.Api.Infrastructure;

public sealed class CatalogSeeder(CatalogDbContext db, CourseSearch search, ILogger<CatalogSeeder> logger)
{
    private static readonly Guid AlgorithmsId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
    private static readonly Guid LinearId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2");
    private static readonly Guid DistributedId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3");

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await CatalogSchema.EnsureAsync(db, cancellationToken);
        await db.Courses
            .Where(c => c.TenantId == Guid.Empty)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.TenantId, Tenancy.DefaultTenantId), cancellationToken);
        await EnsureSubjectsAsync(cancellationToken);
        await EnsureCoursesAsync(cancellationToken);
        await SeedCurriculumAsync(cancellationToken);
        await EnsureVideoUrlsAsync(cancellationToken);
        await SeedCommunityAsync(cancellationToken);
        await SeedQuizzesAsync(cancellationToken);
        await search.RebuildAsync(db, cancellationToken);
        logger.LogInformation(
            "Catalog seed completed with {CourseCount} courses",
            await db.Courses.CountAsync(cancellationToken));
    }

    private async Task EnsureSubjectsAsync(CancellationToken ct)
    {
        var existing = await db.Subjects.Select(s => s.Code).ToListAsync(ct);
        foreach (var subject in Subjects)
        {
            if (existing.Contains(subject.Code, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            db.Subjects.Add(subject);
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureCoursesAsync(CancellationToken ct)
    {
        var subjects = await db.Subjects.ToDictionaryAsync(s => s.Code, StringComparer.OrdinalIgnoreCase, ct);
        var existing = await db.Courses.ToDictionaryAsync(c => c.Id, ct);

        foreach (var seed in Courses)
        {
            if (!subjects.TryGetValue(seed.SubjectCode, out var subject))
            {
                throw new InvalidOperationException($"Seed subject {seed.SubjectCode} is missing.");
            }

            if (existing.TryGetValue(seed.Id, out var course))
            {
                if (string.IsNullOrWhiteSpace(course.Subtitle))
                {
                    course.Subtitle = seed.Subtitle;
                    course.Level = seed.Level;
                    course.Language = "English";
                    course.Outcomes = seed.Outcomes;
                    course.Requirements = seed.Requirements;
                }

                if (course.Status != CourseStatus.Published)
                {
                    course.Status = CourseStatus.Published;
                    course.PublishedAt ??= DateTimeOffset.UtcNow;
                    if (course.RemainingSeats <= 0)
                    {
                        course.RemainingSeats = course.Capacity;
                    }
                }

                continue;
            }

            db.Courses.Add(new Course
            {
                Id = seed.Id,
                SubjectId = subject.Id,
                Title = seed.Title,
                Subtitle = seed.Subtitle,
                Description = seed.Description,
                Level = seed.Level,
                Language = "English",
                Outcomes = seed.Outcomes,
                Requirements = seed.Requirements,
                TeacherId = SeedUsers.TeacherId,
                TeacherName = "Ava Teacher",
                TeacherEmail = SeedUsers.TeacherEmail,
                Capacity = seed.Capacity,
                RemainingSeats = seed.Capacity,
                Price = seed.Price,
                Status = CourseStatus.Published,
                CreatedAt = DateTimeOffset.UtcNow,
                PublishedAt = DateTimeOffset.UtcNow,
                TenantId = Tenancy.DefaultTenantId
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task SeedCurriculumAsync(CancellationToken ct)
    {
        var seededCourseIds = await db.CourseSections.Select(s => s.CourseId).Distinct().ToListAsync(ct);

        if (!seededCourseIds.Contains(AlgorithmsId))
        {
            AddAlgorithmsCurriculum();
        }

        if (!seededCourseIds.Contains(LinearId))
        {
            AddLinearCurriculum();
        }

        foreach (var seed in Courses)
        {
            if (seed.Id == AlgorithmsId || seed.Id == LinearId || seededCourseIds.Contains(seed.Id))
            {
                continue;
            }

            AddDefaultCurriculum(seed);
        }

        await db.SaveChangesAsync(ct);
    }

    private void AddAlgorithmsCurriculum()
    {
        AddSection(AlgorithmsId, 1, "Getting started",
            Lecture(1, "Welcome to the course", "Video", 4, true,
                "How the course is structured and how to use the player, Q&A, and reviews.",
                "Welcome. Each section has a short lecture plus a worked example.\n\nUse Preview lectures before you enroll. After enrollment, the full article body unlocks and you can post reviews and questions.\n\nSuggested pace: two lectures a sitting, then try the practice prompt at the end of each article.",
                "https://www.youtube.com/watch?v=8hly31xKli0"),
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
                "Merge sort is stable and O(n log n) always. It needs extra memory. Quicksort is often faster in practice and can be in-place, but the worst case is quadratic unless you randomize or median-of-three the pivot.\n\nUse merge sort (or a library timsort) when stability matters — for example sorting students by grade, then keeping their original order for equal grades.\n\nPractice: sort 8 integers with merge sort on paper and count the merges.",
                "https://www.youtube.com/watch?v=4VqmGXwpLqc"),
            Lecture(2, "BFS and DFS on campus maps", "Article", 20, false,
                "Traverse graphs with a queue or a stack.",
                "Represent buildings as nodes and walkways as edges. BFS finds the fewest hops. DFS explores a path fully before backtracking.\n\nBFS uses a queue. Mark a node visited when you enqueue it, not when you dequeue it, or you will explode the queue on dense graphs.\n\nDFS can be recursive or an explicit stack. Watch the recursion depth on long corridors.\n\nPractice: from the library, list the BFS order of neighboring buildings if each edge has the same walking time."));
        AddSection(AlgorithmsId, 4, "Dynamic programming",
            Lecture(1, "The DP checklist", "Article", 22, false,
                "Optimal substructure, overlapping subproblems, and a table.",
                "Dynamic programming is recursion plus a cache — or a table filled in an order that makes the cache unnecessary.\n\nChecklist: (1) define the state, (2) write the recurrence, (3) identify the base cases, (4) decide top-down or bottom-up, (5) recover the answer.\n\nClassic starters: Fibonacci, coin change, longest common subsequence, 0/1 knapsack.\n\nPractice: write the recurrence for the number of ways to climb n stairs taking 1 or 2 steps at a time."));
    }

    private void AddLinearCurriculum()
    {
        AddSection(LinearId, 1, "Vectors and spaces",
            Lecture(1, "Vectors you can see", "Video", 8, true,
                "Arrows, coordinates, and why two numbers can mean a point or a direction.",
                "A vector is both a point from the origin and a displacement. Adding vectors is walking one path then another. Scaling stretches or flips it.\n\nIn R² we write columns. The standard basis i and j let you rebuild any vector as a combination.\n\nPractice: draw u = (2, 1) and v = (−1, 2). Sketch u+v and 2u − v.",
                "https://www.youtube.com/watch?v=fNk_zzaMoSs"),
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
                "Av = λv. The vector stays on its line; the scalar λ says stretch or flip.\n\nCharacteristic polynomial det(A − λI) = 0. Multiplicity can be algebraic or geometric — they need not match, which is why not every matrix diagonalizes.\n\nPractice: find eigenvalues of [[2, 1], [0, 3]].",
                "https://www.youtube.com/watch?v=PF_pS2omP3I"),
            Lecture(2, "A first look at SVD", "Article", 16, false,
                "Why data people keep saying “singular values”.",
                "Any matrix factors as UΣVᵀ. Singular values are the stretch amounts. Large singular values are the important directions in a dataset.\n\nYou do not need the full proof in this course. You do need the picture: rotation, stretch, rotation. That picture is principal component analysis in disguise."));
    }

    private void AddDefaultCurriculum(CourseSeed seed)
    {
        AddSection(seed.Id, 1, "Getting started",
            Lecture(1, $"Welcome to {seed.Title}", "Video", 5, true,
                "How the course is structured and what you can preview before you enroll.",
                $"Welcome to {seed.Title}.\n\n{seed.Subtitle}\n\nPreview lectures are open to everyone. After you enroll, the remaining articles unlock and you can post reviews and questions in the course Q&A.\n\nSuggested pace: one section per sitting, then try the practice prompt."),
            Lecture(2, "How this course works", "Article", 9, true,
                "Player, curriculum, and what enrollment unlocks.",
                $"{seed.Description}\n\nUse the curriculum accordion to jump between lectures. Preview items have a badge. The buy card on the landing page becomes Go to course once your enrollment is confirmed."));
        AddSection(seed.Id, 2, "Core lessons",
            Lecture(1, "Foundations", "Article", 16, false,
                $"The ideas that make {seed.Title} click.",
                $"This lesson covers the foundations for {seed.Title} at {seed.Level.ToLowerInvariant()} level.\n\nOutcomes for this course:\n{seed.Outcomes}\n\nRead once for the map, then again with a notebook. Write one example from your own campus life for each outcome."),
            Lecture(2, "A campus worked example", "Article", 14, false,
                "Apply the idea to a situation you will actually meet on campus.",
                $"Worked example for {seed.Title}.\n\nTake a concrete campus scenario — registration queues, lab groups, a club budget, or a research spreadsheet — and apply this week's method end to end.\n\nWrite the inputs, the steps, and how you would check the result. That check is the habit this course is training."),
            Lecture(3, "Practice and next steps", "Video", 8, false,
                "A short recap and what to try before the next section.",
                $"Recap of {seed.Title}.\n\nRequirements coming in:\n{seed.Requirements}\n\nPractice: explain the main idea of this course to a classmate in five sentences, then list one thing you would still look up. Bring that question to the Q&A."));
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

    private static Lecture Lecture(int order, string title, string kind, int minutes, bool preview, string summary, string body, string? videoUrl = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Kind = kind,
            DurationMinutes = minutes,
            IsPreview = preview,
            SortOrder = order,
            Summary = summary,
            Body = body,
            VideoUrl = string.Equals(kind, "Video", StringComparison.OrdinalIgnoreCase)
                ? videoUrl ?? DemoVideo(order + title.Length)
                : null
        };

    private static readonly string[] DemoVideos =
    [
        "https://www.youtube.com/watch?v=8hly31xKli0",
        "https://www.youtube.com/watch?v=fNk_zzaMoSs",
        "https://www.youtube.com/watch?v=aircAruvnKk",
        "https://www.youtube.com/watch?v=HXV3zeQKqGY",
        "https://www.youtube.com/watch?v=rfscVS0vtbw",
        "https://www.youtube.com/watch?v=8JJ101D3knE",
        "https://www.youtube.com/watch?v=PkZNo7MFNFg",
        "https://www.youtube.com/watch?v=W6NZfCO5SIk"
    ];

    private static string DemoVideo(int n) => DemoVideos[Math.Abs(n) % DemoVideos.Length];

    private async Task EnsureVideoUrlsAsync(CancellationToken ct)
    {
        var videos = await db.Lectures
            .Where(l => l.Kind == "Video" && (l.VideoUrl == null || l.VideoUrl == ""))
            .ToListAsync(ct);
        if (videos.Count == 0)
        {
            return;
        }

        for (var i = 0; i < videos.Count; i++)
        {
            videos[i].VideoUrl = DemoVideo(i + videos[i].Title.Length);
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task SeedQuizzesAsync(CancellationToken ct)
    {
        if (await db.CourseQuizzes.AnyAsync(ct))
        {
            return;
        }

        var questions = new[]
        {
            new
            {
                id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc01"),
                prompt = "What does O(n log n) typically describe?",
                choices = new[] { "Constant time", "Efficient comparison sorts", "Exponential blow-up", "Linear scans only" },
                correctIndex = 1,
            },
            new
            {
                id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc02"),
                prompt = "BFS finds shortest paths when edges are:",
                choices = new[] { "Weighted with negatives", "Unweighted (or equal weight)", "Directed acyclic only", "Always complete graphs" },
                correctIndex = 1,
            },
            new
            {
                id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc03"),
                prompt = "A hash table's average lookup is:",
                choices = new[] { "O(n²)", "O(log n)", "O(1)", "O(n log n)" },
                correctIndex = 2,
            },
        };

        db.CourseQuizzes.Add(new CourseQuiz
        {
            Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc00"),
            CourseId = AlgorithmsId,
            Title = "Algorithms checkpoint",
            PassPercent = 70,
            QuestionsJson = System.Text.Json.JsonSerializer.Serialize(questions),
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct);
    }

    private async Task SeedCommunityAsync(CancellationToken ct)
    {
        if (!await db.CourseReviews.AnyAsync(r => r.CourseId == AlgorithmsId, ct))
        {
            db.CourseReviews.AddRange(
                Review(AlgorithmsId, SeedUsers.StudentId, "Sam Student", 5, "Clear and practical",
                    "The Big-O lectures finally made worst vs average case click. Preview lessons are honest about what you get after enrolling.", -6),
                Review(AlgorithmsId, "reviewer-jordan", "Jordan Lee", 4, "Great graphs section",
                    "BFS on the campus map example is the kind of intuition I wish every algorithms class used. Wanted one more coding exercise.", -3),
                Review(LinearId, "reviewer-mina", "Mina Patel", 5, "Finally not just formulas",
                    "Eigenvectors as invariant directions is the explanation I will remember. Pacing is fair for an intermediate course.", -4));
        }

        var extraReviews = new (Guid CourseId, string StudentId, string Name, int Rating, string Title, string Body, int DaysAgo)[]
        {
            (DistributedId, "reviewer-noah", "Noah Okonkwo", 5, "Studio that feels real",
                "The distributed systems examples map to services we actually run. Tough but fair.", -5),
            (CourseId(4), "reviewer-priya", "Priya Shah", 4, "Data structures without the fog",
                "Lists, trees, and hash tables with campus examples. I used the preview lectures before enrolling.", -8),
            (CourseId(5), "reviewer-luis", "Luis Romero", 5, "APIs I can ship",
                "ASP.NET sections are practical. The campus worked example is basically our gateway homework.", -2),
            (CourseId(18), "reviewer-elena", "Elena Voss", 4, "Good first data course",
                "Clear on what a dataset is and what it is not. Wanted one more notebook exercise.", -7),
            (CourseId(20), "reviewer-kenji", "Kenji Mori", 5, "ML without the hype",
                "Foundations first, then a honest look at what the model can miss. Rating it five.", -1),
            (CourseId(26), "reviewer-amira", "Amira Haddad", 5, "UX that respects students",
                "The campus product examples are better than generic SaaS case studies.", -9)
        };

        var existingKeys = (await db.CourseReviews
                .Select(r => new { r.CourseId, r.StudentId })
                .ToListAsync(ct))
            .Select(r => (r.CourseId, r.StudentId))
            .ToHashSet();
        foreach (var tracked in db.CourseReviews.Local)
        {
            existingKeys.Add((tracked.CourseId, tracked.StudentId));
        }

        foreach (var item in extraReviews)
        {
            if (existingKeys.Contains((item.CourseId, item.StudentId)))
            {
                continue;
            }

            existingKeys.Add((item.CourseId, item.StudentId));
            db.CourseReviews.Add(Review(item.CourseId, item.StudentId, item.Name, item.Rating, item.Title, item.Body, item.DaysAgo));
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

    private static CourseReview Review(Guid courseId, string studentId, string name, int rating, string title, string body, int daysAgo) =>
        new()
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            StudentId = studentId,
            StudentName = name,
            Rating = rating,
            Title = title,
            Body = body,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(daysAgo)
        };

    private static Guid CourseId(int n) => Guid.Parse($"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb{n:D2}");

    private static readonly Subject[] Subjects =
    [
        new() { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), TenantId = Tenancy.DefaultTenantId, Code = "CS", Name = "Computer Science", Description = "Algorithms, systems, and software engineering." },
        new() { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), TenantId = Tenancy.DefaultTenantId, Code = "MATH", Name = "Mathematics", Description = "Pure and applied mathematics." },
        new() { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"), TenantId = Tenancy.DefaultTenantId, Code = "ENG", Name = "English", Description = "Language, writing, and literature." },
        new() { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4"), TenantId = Tenancy.DefaultTenantId, Code = "DATA", Name = "Data Science", Description = "Data, analysis, and machine learning." },
        new() { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5"), TenantId = Tenancy.DefaultTenantId, Code = "BUS", Name = "Business", Description = "Management, ventures, and personal finance." },
        new() { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa6"), TenantId = Tenancy.DefaultTenantId, Code = "PHYS", Name = "Physics", Description = "Mechanics and physical intuition." },
        new() { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa7"), TenantId = Tenancy.DefaultTenantId, Code = "DES", Name = "Design", Description = "UX and visual design for campus products." },
        new() { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa8"), TenantId = Tenancy.DefaultTenantId, Code = "HIST", Name = "History", Description = "Modern world and civic context." }
    ];

    private static readonly CourseSeed[] Courses =
    [
        new(AlgorithmsId, "CS", "Introduction to Algorithms",
            "Learn analysis, sorting, graphs, and dynamic programming with worked campus examples.",
            "A practical first course in algorithms. You will analyze running time, implement core data structures, and solve interview-style problems with confidence.",
            "Beginner", 49.00m, 30,
            "Analyze time and space complexity with Big-O\nImplement sorting and searching correctly\nModel problems with graphs and BFS/DFS\nSolve classic dynamic programming patterns",
            "Comfortable with loops and arrays in any language\nNo prior algorithms course required"),
        new(LinearId, "MATH", "Linear Algebra",
            "Vector spaces, matrices, and eigenvalues — built for students who will use them in CS and data.",
            "From vectors to eigen-decompositions, this course connects the algebra to geometry and to applications you will meet in machine learning and graphics.",
            "Intermediate", 39.00m, 25,
            "Compute with matrices and linear systems\nExplain vector spaces, bases, and dimension\nFind eigenvalues and interpret them geometrically\nApply SVD intuition to data problems",
            "High-school algebra and a little trigonometry\nA scientific calculator or notebook is enough"),
        new(DistributedId, "CS", "Distributed Systems Studio",
            "Build intuition for replicas, timeouts, and the failures campus services actually hit.",
            "A studio course on distributed systems: consistency, retries, outbox patterns, and why your gateway and identity service cannot share one clock.",
            "Advanced", 79.00m, 15,
            "Name consistency and availability trade-offs\nDesign retries, idempotency, and timeouts\nExplain outbox and saga compensation at a high level\nRead a service diagram without getting lost",
            "Comfortable with HTTP APIs\nA prior systems or databases course helps"),
        new(CourseId(4), "CS", "Data Structures in Practice",
            "Lists, trees, heaps, and hash tables using campus registration as the running example.",
            "Choose the right structure for the job. You will implement the classics and then defend the choice when the waitlist, the catalog, or the chat history grows.",
            "Beginner", 44.00m, 40,
            "Implement lists, stacks, queues, and trees\nUse hash tables without losing your keys\nCompare structures by time and memory\nExplain a choice in a code review",
            "Basic programming in any language"),
        new(CourseId(5), "CS", "Web APIs with ASP.NET",
            "Minimal APIs, contracts, and auth headers the way CampusHub services are shaped.",
            "Design and ship HTTP APIs: routing, validation, problem details, and calling downstream services without leaking tokens to the browser.",
            "Intermediate", 59.00m, 30,
            "Design resource-oriented HTTP APIs\nValidate input and return useful errors\nForward identity with Bearer tokens\nWrite an endpoint you could put behind YARP",
            "C# basics\nCuriosity about how browsers talk to servers"),
        new(CourseId(6), "CS", "TypeScript for Campus Apps",
            "Types that catch bugs before students click Enroll.",
            "TypeScript from types and unions through modules and Angular-shaped components, aimed at students who already know a bit of JavaScript.",
            "Beginner", 35.00m, 45,
            "Model data with interfaces and unions\nAvoid any except when you mean it\nOrganize UI code into small components\nRead compiler errors instead of fighting them",
            "Some JavaScript or another C-like language"),
        new(CourseId(7), "CS", "Databases and SQL",
            "Tables, keys, and queries using a campus catalog as the dataset.",
            "Relational modeling and SQL for students who will touch SQLite, Postgres, or both. Joins, indexes, and why EnsureCreated is not a migration strategy.",
            "Beginner", 42.00m, 40,
            "Write SELECT queries with joins and filters\nModel entities and foreign keys\nExplain indexes without pretending they are magic\nSpot an N+1 before it ships",
            "No prior SQL required"),
        new(CourseId(8), "CS", "Operating Systems",
            "Processes, memory, and files — enough OS to debug a service that will not bind a port.",
            "A first operating systems course focused on what application developers actually meet: processes, threads, virtual memory, and I/O.",
            "Intermediate", 55.00m, 28,
            "Explain processes vs threads\nReason about memory and paging at a high level\nUse files and pipes without leaking handles\nRead a simple concurrency bug",
            "Comfortable writing programs that run more than one function"),
        new(CourseId(9), "CS", "Computer Networks",
            "Packets, TLS, and why localhost:5000 is not the public internet.",
            "Layered networks from sockets to HTTPS, with campus Wi-Fi and reverse proxies as the examples that stick.",
            "Intermediate", 52.00m, 30,
            "Describe TCP vs UDP in plain language\nTrace an HTTP request across a proxy\nExplain TLS at the level a developer needs\nDebug a connection refused vs a 502",
            "Curiosity and a laptop that can run curl or a browser inspector"),
        new(CourseId(10), "CS", "Software Testing that Sticks",
            "Unit, contract, and the tests you write after the bug report.",
            "Testing as a design tool. You will write tests that fail for the right reason and stop writing tests that only assert the mock was called.",
            "Beginner", 38.00m, 35,
            "Write a unit test that names the behavior\nChoose what not to mock\nCover an API contract without a full browser\nTurn a production bug into a regression test",
            "You can write a small function in any language"),
        new(CourseId(11), "MATH", "Calculus I",
            "Limits, derivatives, and the graphs you will see in science and economics.",
            "A first calculus course with campus-paced examples: queues growing, grades curving, and motion on the quad.",
            "Beginner", 41.00m, 40,
            "Compute limits and derivatives of standard functions\nRead a graph as a rate of change\nSolve related-rates problems with a diagram\nUse the first derivative to reason about max and min",
            "Precalculus algebra and functions"),
        new(CourseId(12), "MATH", "Discrete Mathematics",
            "Proofs, sets, and counting for students heading into algorithms.",
            "The language of CS theory without the intimidation: logic, induction, graphs, and counting arguments you will reuse all year.",
            "Intermediate", 46.00m, 32,
            "Write a short direct proof and an induction\nCount with permutations and combinations\nModel a campus process as a graph\nTranslate an English claim into logic",
            "Comfortable with high-school algebra"),
        new(CourseId(13), "MATH", "Probability for Data",
            "Randomness you can compute, not just quote.",
            "Probability as a tool for data students: events, expectation, Bayes, and why a sample is not a census.",
            "Beginner", 40.00m, 36,
            "Compute basic probabilities from a model\nUse expectation and variance as summaries\nApply Bayes to a campus diagnostic example\nSpot a biased sample in a headline",
            "Algebra and a willingness to draw trees"),
        new(CourseId(14), "MATH", "Statistics in Practice",
            "Surveys, confidence, and charts that do not lie on purpose.",
            "Applied statistics for campus research and club surveys. Descriptive stats, intervals, and the difference between significant and important.",
            "Beginner", 37.00m, 40,
            "Summarize a dataset honestly\nBuild a confidence interval you can explain\nDesign a simple survey without leading questions\nRead a p-value without worshipping it",
            "A little probability or a willingness to learn it here"),
        new(CourseId(15), "ENG", "Academic Writing",
            "Claims, evidence, and paragraphs that earn the next sentence.",
            "Write papers that a tired TA can follow. Thesis, structure, citation, and revision as a process instead of a panic the night before.",
            "Beginner", 29.00m, 50,
            "State a claim a reader can disagree with\nSupport it with cited evidence\nRevise for structure before you polish sentences\nUse campus citation style without drowning in it",
            "You can write a paragraph in English"),
        new(CourseId(16), "ENG", "Technical Communication",
            "Docs, RFCs, and emails that engineers will actually read.",
            "Write for busy technical readers: problem statements, design notes, runbooks, and the one-paragraph status update.",
            "Beginner", 32.00m, 45,
            "Lead with the decision and the ask\nDocument an API without copying the code\nWrite a runbook another student can follow\nCut filler that hides the risk",
            "Some experience reading technical material"),
        new(CourseId(17), "ENG", "Public Speaking on Campus",
            "Presentations for labs, clubs, and the 8 a.m. seminar.",
            "Plan a talk, handle a room, and answer questions without freezing. Recorded practice is part of the course.",
            "Beginner", 27.00m, 40,
            "Structure a five-minute talk with one point\nDesign slides that are not a script\nHandle a question you cannot fully answer\nUse notes without reading them",
            "Willingness to practice out loud"),
        new(CourseId(18), "DATA", "Intro to Data Science",
            "Questions, datasets, and the habit of checking the obvious.",
            "A first data science course: asking a question that data can answer, cleaning without destroying, and presenting a result a non-major can trust.",
            "Beginner", 48.00m, 35,
            "Frame a campus question as a data problem\nClean a table without silent row loss\nChoose a chart that matches the claim\nWrite limitations as clearly as findings",
            "Spreadsheets or a little Python help but are not required"),
        new(CourseId(19), "DATA", "Python for Analysis",
            "pandas-shaped thinking even if you start from a notebook.",
            "Python for tables: load, filter, join, group, and export. Aimed at students who will live in notebooks for a semester.",
            "Beginner", 45.00m, 40,
            "Load and inspect a CSV without guessing types\nFilter, group, and join tables\nWrite a reproducible notebook\nExport a result someone else can rerun",
            "Any prior programming helps; absolute beginners can start here slowly"),
        new(CourseId(20), "DATA", "Machine Learning Foundations",
            "Models as functions with failure modes, not magic.",
            "Supervised learning from train/test splits through overfitting, with campus datasets small enough to see what went wrong.",
            "Intermediate", 69.00m, 25,
            "Split data without leaking the future\nFit and evaluate a baseline model\nExplain overfitting in one diagram\nKnow when not to use a model",
            "Python for Analysis or equivalent\nA little probability"),
        new(CourseId(21), "BUS", "Introduction to Management",
            "Teams, goals, and the meetings that should have been an email.",
            "How campus organizations and small teams actually get work done: roles, feedback, and measuring something other than hours logged.",
            "Beginner", 36.00m, 40,
            "Set a goal a team can recognize as done\nRun a short meeting with an agenda\nGive feedback that names the work not the person\nSpot a process that is just theater",
            "No business background required"),
        new(CourseId(22), "BUS", "Campus Entrepreneurship",
            "From club idea to a service students would pay for.",
            "Customer interviews, a thin slice, and pricing without a fantasy spreadsheet. You will pitch a campus problem, not a slogan.",
            "Beginner", 34.00m, 30,
            "Interview users without pitching\nDefine a smallest useful version\nPrice against an alternative students already use\nTell the story in five slides",
            "A problem you have actually watched other students hit"),
        new(CourseId(23), "BUS", "Personal Finance for Students",
            "Aid, rent, and the first paycheck without shame.",
            "Practical money for campus life: budgets, interest, aid refunds, and how subscriptions eat a term.",
            "Beginner", 24.00m, 50,
            "Build a term budget that survives midterms\nCompare interest the honest way\nRead a pay stub\nSpot a fee that is optional if you ask",
            "None — bring a realistic picture of your month"),
        new(CourseId(24), "PHYS", "Physics I: Mechanics",
            "Force, energy, and motion you can sketch.",
            "Newtonian mechanics for scientists and engineers. Free-body diagrams, conservation, and problems that start from a picture.",
            "Beginner", 43.00m, 32,
            "Draw a free-body diagram that matches the story\nApply Newton’s laws to campus-scale motion\nUse energy when forces would be messy\nCheck units before you trust a number",
            "Algebra and basic trigonometry"),
        new(CourseId(25), "PHYS", "Physics of Everyday Things",
            "Why the kettle, the bus, and the phone battery behave that way.",
            "Conceptual physics for non-majors: energy, waves, and electricity with objects you already own.",
            "Beginner", 31.00m, 40,
            "Explain energy transfer in an everyday device\nConnect waves to sound and Wi-Fi at a high level\nRead a power rating without fear\nAsk a better question of a science headline",
            "Curiosity; equations stay light"),
        new(CourseId(26), "DES", "UX for Campus Products",
            "Flows for students who are late, tired, and on a phone.",
            "User experience with campus software as the brief: enrollment, inbox, and the pass they flash at the door.",
            "Beginner", 47.00m, 30,
            "Map a task from intent to done\nSketch a flow before you pick a color\nWrite microcopy that tells the next step\nTest with three students and change something",
            "No Figma required to start"),
        new(CourseId(27), "DES", "Visual Design Basics",
            "Type, space, and contrast so a course card looks like it belongs.",
            "Foundations of visual design for screens: hierarchy, pairing type, and why navy and gold are a system not a gradient.",
            "Beginner", 33.00m, 35,
            "Set type that can be read on a phone\nUse space to group related actions\nBuild contrast that survives a projector\nCritique a screen without only saying pretty",
            "A willingness to look closely"),
        new(CourseId(28), "HIST", "Modern World History",
            "The last two centuries as context for the campus you are standing on.",
            "A survey of modern world history with arguments, not a list of dates. Empires, wars, decolonization, and the institutions that still shape a university.",
            "Beginner", 28.00m, 45,
            "Place a current campus debate in a longer timeline\nRead a primary source without taking it at face value\nWrite a short argument with two pieces of evidence\nCompare two regions without flattening them",
            "Willingness to read and discuss"),
        new(CourseId(29), "CS", "Git and Collaboration",
            "Branches, reviews, and the commit message your teammates can use.",
            "Git as a social tool: clones, branches, pull requests, and how not to force-push main. Aimed at students joining their first real repo.",
            "Beginner", 22.00m, 50,
            "Clone, branch, commit, and open a review\nWrite a commit message that explains why\nResolve a simple conflict without panic\nUse main as a shared history not a scratchpad",
            "A code editor and a GitHub or similar account"),
        new(CourseId(30), "CS", "Mobile-ready Angular",
            "Angular components, routing, and the shell that loads a course MFE.",
            "Build campus UI with Angular: standalone components, signals, and routes that stay fast when the catalog grows to thirty courses.",
            "Beginner", 54.00m, 28,
            "Build a standalone component with a clear input\nRoute a detail page from a catalog grid\nLoad data without blocking the whole shell\nKeep styles consistent with a small design system",
            "TypeScript for Campus Apps or equivalent JavaScript")
    ];

    private readonly record struct CourseSeed(
        Guid Id,
        string SubjectCode,
        string Title,
        string Subtitle,
        string Description,
        string Level,
        decimal Price,
        int Capacity,
        string Outcomes,
        string Requirements);
}
