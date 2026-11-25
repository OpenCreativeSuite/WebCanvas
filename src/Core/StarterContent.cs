namespace WebCanvasDotNet.Core;

/// <summary>Builds the blank and studio-starter project contents.</summary>
public static class StarterContent
{
    public static void Populate(WcProject project)
    {
        project.Manifest.Description = "An editorial studio site with journal posts, authors and case studies.";
        project.Manifest.BaseUrl = "https://asterandoak.studio";

        project.Theme = new ThemeDoc
        {
            Name = "Aster & Oak",
            Accent = "#2f6b4f",
            Background = "#f7f4ee",
            Surface = "#fffdf8",
            Text = "#1d2a22",
            Muted = "#5d6b61",
            Border = "#e2ded3",
            HeadingFont = "Georgia",
            BodyFont = "Segoe UI",
            Radius = 10,
        };

        PageDoc homePage = Home();
        PageDoc workPage = Work();
        PageDoc journalPage = Journal();
        PageDoc aboutPage = About();
        PageDoc contactPage = Contact();

        project.Pages.Add(homePage);
        project.Pages.Add(workPage);
        project.Pages.Add(journalPage);
        project.Pages.Add(aboutPage);
        project.Pages.Add(contactPage);

        var headerBlock = new BlockDoc
        {
            Id = "block_site_header",
            Name = "Site header",
            Description = "Brand and primary navigation. Shared by every page.",
            Kind = "theme",
            Category = "Navigation",
            Nodes = { Header() },
        };

        var footerBlock = new BlockDoc
        {
            Id = "block_site_footer",
            Name = "Site footer",
            Description = "Three-column footer with navigation and contact details.",
            Kind = "theme",
            Category = "Footer",
            Nodes = { Footer() },
        };

        var heroBlock = new BlockDoc
        {
            Id = "block_editorial_hero",
            Name = "Editorial hero",
            Description = "A large editorial opening with supporting artwork.",
            Kind = "content",
            Category = "Hero",
        };

        var featureBlock = new BlockDoc
        {
            Id = "block_feature_trio",
            Name = "Feature trio",
            Description = "Three short value points in a row.",
            Kind = "content",
            Category = "Section",
            Nodes = { FeatureSection() },
        };

        var ctaBlock = new BlockDoc
        {
            Id = "block_dark_cta",
            Name = "Dark call to action",
            Description = "A full-width closing prompt with a single action.",
            Kind = "content",
            Category = "Call to action",
            Nodes =
            {
                Band("Have a project in mind?",
                    "Tell us what you're making. We'll reply with an honest answer about whether we're the right studio for it.",
                    "Start a conversation",
                    "/contact/"),
            },
        };

        PageNode? hero = ReplaceFirst(
            homePage.Nodes,
            node => node.Tag == "section" && (node.ClassName?.Contains("site-hero", StringComparison.Ordinal) ?? false),
            _ => BlockRef(heroBlock));

        if (hero is not null)
        {
            heroBlock.Nodes.Add(hero);
        }

        project.Blocks.Add(headerBlock);
        project.Blocks.Add(footerBlock);
        project.Blocks.Add(heroBlock);
        project.Blocks.Add(featureBlock);
        project.Blocks.Add(ctaBlock);

        foreach (PageDoc page in project.Pages)
        {
            ReplaceAll(page.Nodes, node => node.Tag == "header", () => BlockRef(headerBlock));
            ReplaceAll(page.Nodes, node => node.Tag == "footer", () => BlockRef(footerBlock));
        }

        var author = new DataSchema
        {
            Name = "Author",
            Plural = "authors",
            Icon = "fa-user",
            Description = "The people who write and make the work.",
            Fields =
            {
                new DataField { Name = "name", Type = FieldTypes.Text, Required = true },
                new DataField { Name = "role", Type = FieldTypes.Text },
                new DataField { Name = "bio", Type = FieldTypes.LongText },
                new DataField { Name = "avatar", Type = FieldTypes.Image },
                new DataField { Name = "email", Type = FieldTypes.Text },
            },
        };

        var post = new DataSchema
        {
            Name = "Post",
            Plural = "posts",
            Icon = "fa-newspaper",
            Description = "Journal entries, notes and studio writing.",
            Fields =
            {
                new DataField { Name = "title", Type = FieldTypes.Text, Required = true },
                new DataField { Name = "slug", Type = FieldTypes.Text, Help = "Used for the published URL." },
                new DataField { Name = "excerpt", Type = FieldTypes.LongText },
                new DataField { Name = "body", Type = FieldTypes.RichText },
                new DataField { Name = "publishedAt", Type = FieldTypes.Date },
                new DataField { Name = "author", Type = FieldTypes.Relation, Target = "Author" },
                new DataField { Name = "cover", Type = FieldTypes.Image },
                new DataField { Name = "featured", Type = FieldTypes.Boolean },
            },
        };

        var work = new DataSchema
        {
            Name = "Project",
            Plural = "projects",
            Icon = "fa-layer-group",
            Description = "Case studies shown on the work index.",
            Fields =
            {
                new DataField { Name = "title", Type = FieldTypes.Text, Required = true },
                new DataField { Name = "client", Type = FieldTypes.Text },
                new DataField { Name = "year", Type = FieldTypes.Number },
                new DataField { Name = "summary", Type = FieldTypes.LongText },
                new DataField { Name = "body", Type = FieldTypes.RichText },
                new DataField { Name = "services", Type = FieldTypes.Text },
                new DataField { Name = "image", Type = FieldTypes.Image },
                new DataField { Name = "featured", Type = FieldTypes.Boolean },
            },
        };

        project.Schemas.Add(author);
        project.Schemas.Add(post);
        project.Schemas.Add(work);

        project.Entities["Author"] = new List<EntityDoc>
        {
            Entity("ent_author_mira", Ids.Now(),
                ("name", "Mira Chen"), ("role", "Design lead"),
                ("bio", "Mira leads interface design at the studio. Before this she shaped products at two publishing houses and still keeps a letterpress in the basement."),
                ("email", "mira@asterandoak.studio")),
            Entity("ent_author_jonas", Ids.Now(),
                ("name", "Jonas Reed"), ("role", "Engineering"),
                ("bio", "Jonas builds the systems underneath the work: content models, static pipelines and the small tools that keep editors calm."),
                ("email", "jonas@asterandoak.studio")),
            Entity("ent_author_ines", Ids.Now(),
                ("name", "Inès Duarte"), ("role", "Strategy"),
                ("bio", "Inès helps clients decide what the product is for before anyone opens a design tool."),
                ("email", "ines@asterandoak.studio")),
        };

        project.Entities["Post"] = new List<EntityDoc>
        {
            Entity("ent_post_second_visit", "2026-11-18T09:20:00.0000000Z",
                ("title", "Designing for the second visit"),
                ("slug", "designing-for-the-second-visit"),
                ("excerpt", "Most interfaces are judged on the second visit. The first impression gets all the attention; the second visit decides whether the product is actually good."),
                ("body", "The second visit is where a product becomes a habit. The navigation has to be remembered, the content has to be where you left it, and the next action has to be obvious without any of the ceremony of onboarding.\n\nWe design a second-visit pass for every project. It is deliberately unglamorous: fewer decisions, better defaults, and a clear place to return to."),
                ("publishedAt", "2026-11-18"),
                ("author", "ent_author_mira"),
                ("featured", "true")),
            Entity("ent_post_content_model", "2026-10-30T14:05:00.0000000Z",
                ("title", "The quiet power of a content model"),
                ("slug", "quiet-power-of-a-content-model"),
                ("excerpt", "A good content model removes more interface than it adds. Name the things you actually have, and half the screens stop being necessary."),
                ("body", "Every CMS begins as a list of things. The useful work is deciding which things are real, which are views of the same thing, and which only exist because a form made them feel real.\n\nWhen the model is honest, the editor becomes small."),
                ("publishedAt", "2026-10-30"),
                ("author", "ent_author_jonas")),
            Entity("ent_post_prototypes", "2026-10-12T10:40:00.0000000Z",
                ("title", "Prototypes that survive contact with reality"),
                ("slug", "prototypes-that-survive-contact-with-reality"),
                ("excerpt", "A prototype is a question with a shape. The good ones answer something specific and then get out of the way."),
                ("body", "We keep prototypes close to the real content. Real words, real lengths, real edge cases. The polish comes later; the truth comes first."),
                ("publishedAt", "2026-10-12"),
                ("author", "ent_author_mira"),
                ("featured", "true")),
            Entity("ent_post_editorial_spacing", "2026-09-24T08:15:00.0000000Z",
                ("title", "A field guide to editorial spacing"),
                ("slug", "field-guide-to-editorial-spacing"),
                ("excerpt", "Spacing is not the space between things. It is the relationship between them, made visible."),
                ("body", "We use a small set of spacing steps and a lot of restraint. The goal is rhythm, not uniformity: a paragraph and a section heading should not breathe at the same interval."),
                ("publishedAt", "2026-09-24"),
                ("author", "ent_author_jonas")),
        };

        project.Entities["Project"] = new List<EntityDoc>
        {
            Entity("ent_project_northline", "2026-11-02T11:00:00.0000000Z",
                ("title", "Northline"),
                ("client", "Northline Freight"),
                ("year", "2026"),
                ("summary", "A dispatch platform for a regional freight company, designed around the rhythm of a shift rather than a dashboard."),
                ("body", "Northline moves hundreds of loads a day. The old system made dispatchers hold the schedule in their heads. The new one puts the next decision on screen and keeps everything else quiet."),
                ("services", "Product strategy, Interface design, Design system"),
                ("featured", "true")),
            Entity("ent_project_fieldwork", "2026-08-21T15:30:00.0000000Z",
                ("title", "Fieldwork"),
                ("client", "Fieldwork Health"),
                ("year", "2025"),
                ("summary", "A calmer way for community health teams to record visits, follow up and hand work to each other."),
                ("body", "Fieldwork is used in clinics with unreliable networks and very little time. The interface is offline-first, thumb-sized and deliberately plain."),
                ("services", "Research, Product design, Content model"),
                ("featured", "true")),
            Entity("ent_project_lumen", "2026-05-09T09:45:00.0000000Z",
                ("title", "Lumen"),
                ("client", "Lumen Books"),
                ("year", "2025"),
                ("summary", "An independent bookshop's catalogue, events programme and reading notes in one editorial system."),
                ("body", "Lumen needed to publish quickly without losing the voice of the shop. We built a small set of content types and let the writing carry the design."),
                ("services", "Brand systems, Website, CMS"),
                ("featured", "true")),
            Entity("ent_project_sable", "2025-11-14T13:10:00.0000000Z",
                ("title", "Sable"),
                ("client", "Sable & Co"),
                ("year", "2024"),
                ("summary", "A materials library and specification tool for an architectural studio."),
                ("body", "Sable turns a filing cabinet of samples into a searchable, visual library that architects can use during a project meeting."),
                ("services", "Interface design, Data modelling"),
                ("featured", "false")),
        };

        project.Handlers.Add(new HandlerDoc
        {
            Id = "handler_log_posts",
            Name = "Log every new journal entry",
            Event = "entity.created",
            EntityType = "Post",
            Enabled = true,
            Actions =
            {
                new ActionDoc { Type = "log", Value = "New journal entry created." },
            },
        });

        project.Handlers.Add(new HandlerDoc
        {
            Id = "handler_notify_featured",
            Name = "Notify the studio when a feature goes live",
            Event = "entity.updated",
            EntityType = "Post",
            Enabled = true,
            Conditions =
            {
                new ConditionDoc { Field = "featured", Operator = "equals", Value = "true" },
            },
            Actions =
            {
                new ActionDoc { Type = "email", Target = "studio@asterandoak.studio", Value = "A featured journal entry was updated." },
                new ActionDoc { Type = "log", Value = "Featured entry notification queued." },
            },
        });

        project.Handlers.Add(new HandlerDoc
        {
            Id = "handler_build_sitemap",
            Name = "Refresh the search index after a build",
            Event = "site.built",
            Enabled = true,
            Actions =
            {
                new ActionDoc { Type = "log", Value = "Search index refreshed." },
            },
        });

        project.AddExamplePlugin();
    }

    public static void PopulateBlank(WcProject project)
    {
        project.Manifest.Description = "A new WebCanvas site.";
        project.Theme = new ThemeDoc
        {
            Name = "Studio",
            Accent = "#2456c7",
            Background = "#f6f7f9",
            Surface = "#ffffff",
            Text = "#171b22",
            Muted = "#5d6675",
            Border = "#e1e5eb",
            HeadingFont = "Segoe UI",
            BodyFont = "Segoe UI",
            Radius = 8,
        };

        var page = new PageDoc
        {
            Id = "page_home",
            Title = "Home",
            Slug = "",
            Description = "",
            NavLabel = "",
            ShowInNav = true,
            Nodes =
            {
                Header(brand: project.Manifest.Name),
                PageNode.Create("main", null, null, null,
                    PageNode.Create("section", "site-hero site-shell", null, null,
                        PageNode.Create("div", "hero-main", null, new Dictionary<string, string> { ["max-width"] = "760px" },
                            PageNode.Create("span", "eyebrow", "A new site"),
                            PageNode.Create("h1", "hero-title", "Start with something worth reading."),
                            PageNode.Create("p", "hero-copy", "This page is yours. Add sections, write the words, shape the type — then publish it as a fast static site."),
                            PageNode.Create("div", "hero-actions", null, null,
                                Linked("a", "site-btn", "Edit this page", "#"),
                                Linked("a", "site-btn ghost", "Open the data workspace", "#"))))),
                PageNode.Create("section", "site-section site-shell", null, null,
                    PageNode.Create("div", "section-head", null, null,
                        PageNode.Create("div", null, null, null,
                            PageNode.Create("span", "section-kicker", "Getting started"),
                            PageNode.Create("h2", "section-title", "Three quiet places to begin."))),
                    PageNode.Create("div", "feature-grid", null, null,
                        Feature("01", "Write the content", "Open Content to create your first data type, or start with the pages you already have."),
                        Feature("02", "Shape the design", "Select any block on this page to adjust spacing, type and colour in the inspector."),
                        Feature("03", "Publish it", "The Development workspace builds a static site you can host anywhere."))),
                PageNode.Create("section", "site-band", null, null,
                    PageNode.Create("div", "site-shell site-band-inner", null, null,
                        PageNode.Create("div", "site-band-copy", null, null,
                            PageNode.Create("h2", "section-title", "Everything you publish is just files."),
                            PageNode.Create("p", "section-lead", "No database to host, no runtime to maintain. Your content stays readable and yours.")),
                        Linked("a", "site-btn", "Open Development", "#"))),
                Footer(project.Manifest.Name, "A new site, made with WebCanvas."),
            },
        };

        project.Pages.Add(page);

        var headerBlock = new BlockDoc
        {
            Id = "block_site_header",
            Name = "Site header",
            Description = "Brand and navigation. Shared by every page.",
            Kind = "theme",
            Category = "Navigation",
            Nodes = { Header(brand: project.Manifest.Name) },
        };

        var footerBlock = new BlockDoc
        {
            Id = "block_site_footer",
            Name = "Site footer",
            Description = "The closing footer. Shared by every page.",
            Kind = "theme",
            Category = "Footer",
            Nodes = { Footer(project.Manifest.Name, "A new site, made with WebCanvas.") },
        };

        project.Blocks.Add(headerBlock);
        project.Blocks.Add(footerBlock);
        ReplaceAll(page.Nodes, node => node.Tag == "header", () => BlockRef(headerBlock));
        ReplaceAll(page.Nodes, node => node.Tag == "footer", () => BlockRef(footerBlock));
    }

    // ------------------------------------------------------------------ pages

    private static PageDoc Home() => new()
    {
        Id = "page_home",
        Title = "Home",
        Slug = "",
        Description = "Independent design studio for calm, consequential digital products.",
        NavLabel = "Home",
        Nodes =
        {
            Header(),
            PageNode.Create("main", null, null, null,
                PageNode.Create("section", "site-hero site-shell", null, null,
                    PageNode.Create("div", "hero-grid", null, null,
                        PageNode.Create("div", "hero-main", null, null,
                            PageNode.Create("span", "eyebrow", "Independent design studio · Portland"),
                            PageNode.Create("h1", "hero-title", "Interfaces that feel inevitable."),
                            PageNode.Create("p", "hero-copy", "We design and build calm digital products for teams doing difficult, useful work. Small enough to stay close to the details; experienced enough to ship."),
                            PageNode.Create("div", "hero-actions", null, null,
                                Linked("a", "site-btn", "Start a project", "/contact/"),
                                Linked("a", "site-btn ghost", "See selected work", "/work/")),
                            PageNode.Create("div", "trust-row", null, null,
                                Trust("12 years independent"),
                                Trust("Product, brand and content"),
                                Trust("Portland · working worldwide"))),
                        HeroArt())),
                PageNode.Create("section", "site-section site-shell", null, null,
                    PageNode.Create("div", "section-head", null, null,
                        PageNode.Create("div", null, null, null,
                            PageNode.Create("span", "section-kicker", "What we do"),
                            PageNode.Create("h2", "section-title", "A small team for consequential work.")),
                        PageNode.Create("p", "section-lead", "Strategy, interface design and the engineering to make it real. One team from the first sketch to the last deploy.")),
                    PageNode.Create("div", "feature-grid", null, null,
                        Feature("01", "Product design", "Interfaces that explain themselves. We work in the real content, early and often."),
                        Feature("02", "Content systems", "Models that fit the business, so editors spend their time writing rather than managing the CMS."),
                        Feature("03", "Build and ship", "Static output, fast pages and a publishing pipeline the whole team can understand."))),
                PageNode.Create("section", "site-section site-shell", null, null,
                    PageNode.Create("div", "section-head", null, null,
                        PageNode.Create("div", null, null, null,
                            PageNode.Create("span", "section-kicker", "Selected work"),
                            PageNode.Create("h2", "section-title", "Recent projects.")),
                        Linked("a", "site-btn ghost small", "All projects", "/work/")),
                    Collection("Project", 3)),
                PageNode.Create("section", "site-section site-shell", null, null,
                    PageNode.Create("div", "section-head", null, null,
                        PageNode.Create("div", null, null, null,
                            PageNode.Create("span", "section-kicker", "Journal"),
                            PageNode.Create("h2", "section-title", "Notes from the studio.")),
                        Linked("a", "site-btn ghost small", "Read the journal", "/journal/")),
                    Collection("Post", 3)),
                Band("Have a project in mind?", "Tell us what you're making. We'll reply with an honest answer about whether we're the right studio for it.", "Start a conversation", "/contact/"),
                Footer()),
        },
    };

    private static PageDoc Work() => new()
    {
        Id = "page_work",
        Title = "Work",
        Slug = "work",
        Description = "Selected product, brand and content projects.",
        NavLabel = "Work",
        Nodes =
        {
            Header("work"),
            PageNode.Create("main", null, null, null,
                PageNode.Create("section", "page-head site-shell", null, null,
                    PageNode.Create("span", "eyebrow", "Selected work"),
                    PageNode.Create("h1", "page-title", "Projects with a long life."),
                    PageNode.Create("p", "page-lead", "A few of the products, catalogues and tools we've helped bring into the world. Each one began with a question about how people actually work.")),
                PageNode.Create("section", "site-section site-shell", null, null,
                    Collection("Project", 12)),
                Band("Next in the studio.", "We take on a small number of projects each year. If yours is one of them, we'd like to hear about it.", "Start a project", "/contact/"),
                Footer()),
        },
    };

    private static PageDoc Journal() => new()
    {
        Id = "page_journal",
        Title = "Journal",
        Slug = "journal",
        Description = "Notes on design, content and building calm software.",
        NavLabel = "Journal",
        Nodes =
        {
            Header("journal"),
            PageNode.Create("main", null, null, null,
                PageNode.Create("section", "page-head site-shell", null, null,
                    PageNode.Create("span", "eyebrow", "Journal"),
                    PageNode.Create("h1", "page-title", "Notes from the studio."),
                    PageNode.Create("p", "page-lead", "Occasional writing about interface design, content models and the quiet parts of building software.")),
                PageNode.Create("section", "site-section site-shell", null, null,
                    Collection("Post", 12)),
                Band("Get the next note.", "We send one short letter each month: what we're working on, what we're reading, and one idea worth keeping.", "Subscribe", "/contact/"),
                Footer()),
        },
    };

    private static PageDoc About()
    {
        PageNode story = PageNode.Create("div", "about-copy prose", null, null,
            PageNode.Create("h2", null, "Close to the work."),
            PageNode.Create("p", null, "We keep the studio small on purpose. The people in the first conversation are the people doing the work, and the work stays visible from the first sketch to the last release."),
            PageNode.Create("p", null, "Most of our projects combine a product interface with a content system. That pairing is where we do our best work: a clear model underneath, a calm surface on top."),
            PageNode.Create("h2", null, "How we work."),
            PageNode.Create("p", null, "Short cycles, real content, and a prototype you can use rather than a document you have to imagine. We would rather discover the hard question in week two than week twelve."),
            PageNode.Create("div", "stat-row", null, null,
                Stat("12", "years independent"),
                Stat("38", "products shipped"),
                Stat("3", "people, on purpose")));

        PageNode aside = PageNode.Create("div", "about-side", null, null,
            PageNode.Create("blockquote", "quote", null, null,
                PageNode.Create("span", null, "“The best interface is the one you stop noticing. Everything else is maintenance.”"),
                PageNode.Create("cite", null, "Mira Chen, design lead")));

        PageNode storySection = PageNode.Create("section", "site-section site-shell", null, null,
            PageNode.Create("div", "about-grid", null, null, story, aside));

        PageNode intro = PageNode.Create("section", "page-head site-shell", null, null,
            PageNode.Create("span", "eyebrow", "About the studio"),
            PageNode.Create("h1", "page-title", "We make software that earns its place."),
            PageNode.Create("p", "page-lead", "Aster & Oak is a three-person studio in Portland. We work with teams who care about the details and want a partner who will stay through the difficult middle."));

        PageNode people = PageNode.Create("section", "site-section site-shell", null, null,
            PageNode.Create("div", "section-head", null, null,
                PageNode.Create("div", null, null, null,
                    PageNode.Create("span", "section-kicker", "The people"),
                    PageNode.Create("h2", "section-title", "Small, senior, present."))),
            Collection("Author", 6));

        PageNode main = PageNode.Create("main", null, null, null, intro, storySection, people);

        return new PageDoc
        {
            Id = "page_about",
            Title = "About",
            Slug = "about",
            Description = "A small independent studio for product, brand and content.",
            NavLabel = "About",
            Nodes =
            {
                Header("about"),
                main,
                Band("Come work with us.", "We're always glad to meet people who care about the craft. Tell us what you're making.", "Say hello", "/contact/"),
                Footer(),
            },
        };
    }

    private static PageDoc Contact()
    {
        PageNode form = PageNode.Create("div", "contact-form", null, null,
            Field("Name", "Your name", "input"),
            Field("Email", "you@company.com", "input"),
            Field("Project", "What are you hoping to make?", "textarea"),
            PageNode.Create("div", null, null, null,
                Linked("a", "site-btn", "Send the note", "mailto:studio@asterandoak.studio")));

        PageNode details = PageNode.Create("div", "contact-side", null, null,
            PageNode.Create("div", "detail-list", null, null,
                Detail("Email", "studio@asterandoak.studio"),
                Detail("Studio", "Portland, Oregon"),
                Detail("Hours", "Monday to Thursday, 9–5 Pacific"),
                Detail("New work", "Two project slots open for spring 2027")));

        PageNode contactGrid = PageNode.Create("div", "contact-grid", null, null, form, details);

        PageNode intro = PageNode.Create("section", "page-head site-shell", null, null,
            PageNode.Create("span", "eyebrow", "Contact"),
            PageNode.Create("h1", "page-title", "Tell us what you're making."),
            PageNode.Create("p", "page-lead", "A short note is enough. We'll reply within two working days with an honest answer about fit, timing and the next step."));

        PageNode body = PageNode.Create("section", "site-section site-shell", null, null, contactGrid);
        PageNode main = PageNode.Create("main", null, null, null, intro, body);

        return new PageDoc
        {
            Id = "page_contact",
            Title = "Contact",
            Slug = "contact",
            Description = "Start a project or ask the studio a question.",
            NavLabel = "Contact",
            Nodes =
            {
                Header("contact"),
                main,
                Footer(),
            },
        };
    }

    // ---------------------------------------------------------------- pieces

    private static PageNode Header(string? activeSlug = null, string brand = "Aster & Oak")
    {
        var nav = PageNode.Create("nav", "site-nav");
        nav.Attrs = new Dictionary<string, string> { ["nav"] = "main" };

        if (activeSlug is not null)
        {
            nav.Attrs["active"] = activeSlug;
        }

        return PageNode.Create("header", "site-header", null, null,
            PageNode.Create("div", "site-shell site-header-inner", null, null,
                Linked("a", "site-brand", null, "/",
                    PageNode.Create("span", "site-mark"),
                    PageNode.Create("span", null, brand)),
                nav));
    }

    private static PageNode Linked(string tag, string? className, string? text, string href, params PageNode[] children)
    {
        var node = PageNode.Create(tag, className, text, null, children);
        node.Attrs = new Dictionary<string, string> { ["href"] = href };
        return node;
    }

    private static PageNode Footer(string brand = "Aster & Oak", string tagline = "A small design studio for calm, consequential software.")
    {
        var links = PageNode.Create("div", "footer-links");
        links.Attrs = new Dictionary<string, string> { ["nav"] = "footer" };
        return PageNode.Create("footer", "site-footer", null, null,
            PageNode.Create("div", "site-shell", null, null,
                PageNode.Create("div", "footer-grid", null, null,
                    PageNode.Create("div", "footer-col", null, null,
                        PageNode.Create("div", "footer-brand", brand),
                        PageNode.Create("div", null, tagline)),
                    PageNode.Create("div", "footer-col", null, null,
                        PageNode.Create("div", "detail-label", "Explore"),
                        links),
                    PageNode.Create("div", "footer-col", null, null,
                        PageNode.Create("div", "detail-label", "Say hello"),
                        PageNode.Create("div", null, "studio@asterandoak.studio"),
                        PageNode.Create("div", null, "Portland, Oregon"))),
                PageNode.Create("div", "footer-fine", "© 2026 Aster & Oak. Built with WebCanvas.")));
    }

    private static PageNode HeroArt() => PageNode.Create("div", "hero-art", null, null,
        PageNode.Create("div", "art-frame", null, null,
            PageNode.Create("div", "art-bar", null, null,
                PageNode.Create("span", "art-dot"),
                PageNode.Create("span", "art-dot"),
                PageNode.Create("span", "art-dot")),
            PageNode.Create("div", "art-head"),
            PageNode.Create("div", "art-line"),
            PageNode.Create("div", "art-line is-short"),
            PageNode.Create("div", "art-block"),
            PageNode.Create("div", "art-cards", null, null,
                PageNode.Create("div", "art-card"),
                PageNode.Create("div", "art-card"),
                PageNode.Create("div", "art-card"))));

    private static PageNode FeatureSection() => PageNode.Create("section", "site-section site-shell", null, null,
        PageNode.Create("div", "section-head", null, null,
            PageNode.Create("div", null, null, null,
                PageNode.Create("span", "section-kicker", "What we do"),
                PageNode.Create("h2", "section-title", "A small team for consequential work.")),
            PageNode.Create("p", "section-lead", "Strategy, interface design and the engineering to make it real. One team from the first sketch to the last deploy.")),
        PageNode.Create("div", "feature-grid", null, null,
            Feature("01", "Product design", "Interfaces that explain themselves. We work in the real content, early and often."),
            Feature("02", "Content systems", "Models that fit the business, so editors spend their time writing rather than managing the CMS."),
            Feature("03", "Build and ship", "Static output, fast pages and a publishing pipeline the whole team can understand.")));

    private static PageNode BlockRef(BlockDoc block)
    {
        var node = PageNode.Create("block");
        node.Attrs = new Dictionary<string, string> { ["block"] = block.Id };
        return node;
    }

    private static PageNode? ReplaceFirst(
        List<PageNode> nodes,
        Func<PageNode, bool> match,
        Func<PageNode, PageNode> replace)
    {
        for (int index = 0; index < nodes.Count; index++)
        {
            if (match(nodes[index]))
            {
                PageNode original = nodes[index];
                nodes[index] = replace(original);
                return original;
            }

            if (nodes[index].Children is { } children && ReplaceFirst(children, match, replace) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private static void ReplaceAll(List<PageNode> nodes, Func<PageNode, bool> match, Func<PageNode> replace)
    {
        for (int index = 0; index < nodes.Count; index++)
        {
            if (match(nodes[index]))
            {
                nodes[index] = replace();
                continue;
            }

            if (nodes[index].Children is { } children)
            {
                ReplaceAll(children, match, replace);
            }
        }
    }

    private static PageNode Feature(string icon, string title, string copy) => PageNode.Create("div", "feature", null, null,
        PageNode.Create("div", "feature-icon", icon),
        PageNode.Create("h3", "feature-title", title),
        PageNode.Create("p", "feature-copy", copy));

    private static PageNode Collection(string schema, int limit)
    {
        var node = PageNode.Create("collection", "collection-grid");
        node.Attrs = new Dictionary<string, string>
        {
            ["type"] = schema,
            ["limit"] = limit.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };

        return node;
    }

    private static PageNode Band(string title, string copy, string button, string href) => PageNode.Create("section", "site-band", null, null,
        PageNode.Create("div", "site-shell site-band-inner", null, null,
            PageNode.Create("div", "site-band-copy", null, null,
                PageNode.Create("h2", "section-title", title),
                PageNode.Create("p", "section-lead", copy)),
            Linked("a", "site-btn", button, href)));

    private static PageNode Trust(string text) => PageNode.Create("span", null, null, null,
        PageNode.Create("span", "trust-dot"),
        PageNode.Create("span", null, text));

    private static PageNode Stat(string value, string label) => PageNode.Create("div", "stat", null, null,
        PageNode.Create("strong", null, value),
        PageNode.Create("span", null, label));

    private static PageNode Field(string label, string placeholder, string kind)
    {
        var input = PageNode.Create(kind == "textarea" ? "textarea" : "input", null);
        input.Attrs = new Dictionary<string, string> { ["placeholder"] = placeholder };

        if (kind != "textarea")
        {
            input.Attrs["type"] = "text";
        }

        return PageNode.Create("div", "field", null, null,
            PageNode.Create("label", null, label),
            input);
    }

    private static PageNode Detail(string label, string value) => PageNode.Create("div", null, null, null,
        PageNode.Create("div", "detail-label", label),
        PageNode.Create("div", "detail-value", value));

    private static EntityDoc Entity(string id, string updated, params (string Key, string Value)[] values)
    {
        var entity = new EntityDoc
        {
            Id = id,
            CreatedAt = updated,
            UpdatedAt = updated,
        };

        foreach (var (key, value) in values)
        {
            entity.Values[key] = value;
        }

        return entity;
    }
}
