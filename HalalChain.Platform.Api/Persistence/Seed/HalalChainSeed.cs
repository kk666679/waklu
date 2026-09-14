using HalalChain.Domain.Halal;
using HalalChain.Platform.Contracts.Halal.Dto;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Platform.Api.Persistence.Seed;

public static class HalalChainSeed
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        // Categories
        var foodCat = new Category { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), Name = "Food & Beverages", Slug = "food-beverages" };
        var snackCat = new Category { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), Name = "Snacks", Slug = "snacks", ParentId = foodCat.Id };
        var drinkCat = new Category { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), Name = "Beverages", Slug = "beverages", ParentId = foodCat.Id };
        var meatCat = new Category { Id = Guid.Parse("10000000-0000-0000-0000-000000000004"), Name = "Meat & Poultry", Slug = "meat-poultry", ParentId = foodCat.Id };
        var spiceCat = new Category { Id = Guid.Parse("10000000-0000-0000-0000-000000000005"), Name = "Spices & Condiments", Slug = "spices-condiments", ParentId = foodCat.Id };
        var bakeryCat = new Category { Id = Guid.Parse("10000000-0000-0000-0000-000000000006"), Name = "Bakery", Slug = "bakery", ParentId = foodCat.Id };
        var cosCat = new Category { Id = Guid.Parse("20000000-0000-0000-0000-000000000001"), Name = "Cosmetics", Slug = "cosmetics" };
        var skinCat = new Category { Id = Guid.Parse("20000000-0000-0000-0000-000000000002"), Name = "Skincare", Slug = "skincare", ParentId = cosCat.Id };
        var hairCat = new Category { Id = Guid.Parse("20000000-0000-0000-0000-000000000003"), Name = "Haircare", Slug = "haircare", ParentId = cosCat.Id };
        var suppCat = new Category { Id = Guid.Parse("30000000-0000-0000-0000-000000000001"), Name = "Supplements", Slug = "supplements" };
        modelBuilder.Entity<Category>().HasData(foodCat, snackCat, drinkCat, meatCat, spiceCat, bakeryCat, cosCat, skinCat, hairCat, suppCat);

        // Vendors
        var v1 = new Vendor { Id = Guid.Parse("A0000000-0000-0000-0000-000000000001"), Name = "Selera Masak", Slug = "selera-masak", Status = "Active", Country = "MY" };
        var v2 = new Vendor { Id = Guid.Parse("A0000000-0000-0000-0000-000000000002"), Name = "Al-Barakah Foods", Slug = "al-barakah", Status = "Active", Country = "MY" };
        var v3 = new Vendor { Id = Guid.Parse("A0000000-0000-0000-0000-000000000003"), Name = "Nusantara Herbal", Slug = "nusantara", Status = "Active", Country = "ID" };
        var v4 = new Vendor { Id = Guid.Parse("A0000000-0000-0000-0000-000000000004"), Name = "PureBite SG", Slug = "purebite", Status = "Active", Country = "SG" };
        var v5 = new Vendor { Id = Guid.Parse("A0000000-0000-0000-0000-000000000005"), Name = "Gulf Halal Trading", Slug = "gulf-halal", Status = "Active", Country = "AE" };
        modelBuilder.Entity<Vendor>().HasData(v1, v2, v3, v4, v5);
        // Products (50)
        var products = new List<Product>();
        var now = new DateTimeOffset(2026, 8, 25, 0, 0, 0, TimeSpan.Zero);
        int idx = 0;
        void AddProduct(string title, string slug, string desc, Guid catId, Guid vendorId, decimal price, string currency, int inv, string origin)
        { idx++; products.Add(new Product { Id = Guid.Parse($"B{idx:D3}0000-0000-0000-0000-000000000000"), Title = title, Slug = slug, Description = desc, CategoryId = catId, VendorId = vendorId, Price = price, Currency = currency, Inventory = inv, Origin = origin, CreatedAt = now.AddDays(-idx) }); }
        // Snacks
        AddProduct("Keropok Lekor Original","keropok-lekor","Traditional fish cracker",snackCat.Id,v1.Id,8.50m,"MYR",500,"Malaysia");
        AddProduct("Keropok Lekor Spicy","keropok-spicy","Spicy variant with chili",snackCat.Id,v1.Id,9.00m,"MYR",300,"Malaysia");
        AddProduct("Murukku Masala","murukku","Crispy Indian ring snack",snackCat.Id,v2.Id,6.50m,"MYR",800,"Malaysia");
        AddProduct("Kueh Lapis Premium","kueh-lapis","Layered cake with coconut milk",snackCat.Id,v2.Id,15.00m,"MYR",120,"Malaysia");
        AddProduct("Tempeh Chips","tempeh-chips","Crunchy fermented soybean chips",snackCat.Id,v3.Id,7.00m,"IDR",600,"Indonesia");
        AddProduct("Rengginang Udang","rengginang","Rice cracker with shrimp",snackCat.Id,v3.Id,5.50m,"IDR",400,"Indonesia");
        AddProduct("Tau Huay Crisps","tau-huay","Crispy tofu snack",snackCat.Id,v4.Id,4.50m,"SGD",350,"Singapore");
        AddProduct("Baklava Assorted","baklava","Pistachio and walnut baklava",snackCat.Id,v5.Id,28.00m,"AED",100,"UAE");
        AddProduct("Namkeen Mix","namkeen","Savory snack mix",snackCat.Id,v5.Id,12.00m,"AED",200,"UAE");
        AddProduct("Peanut Brittle","peanut-brittle","Peanut brittle with palm sugar",snackCat.Id,v1.Id,11.00m,"MYR",150,"Malaysia");
        // Beverages
        AddProduct("Teh Tarik Concentrate","teh-tarik","Instant teh tarik 10 sachets",drinkCat.Id,v1.Id,12.00m,"MYR",400,"Malaysia");
        AddProduct("Kopi O Kosong","kopi-o","Black coffee 15 packs",drinkCat.Id,v1.Id,14.00m,"MYR",350,"Malaysia");
        AddProduct("Sirap Bandung","sirap-bandung","Rose syrup 500ml",drinkCat.Id,v2.Id,8.00m,"MYR",250,"Malaysia");
        AddProduct("Kunyit Asam","kunyit-asam","Turmeric tamarind drink",drinkCat.Id,v3.Id,9.50m,"IDR",300,"Indonesia");
        AddProduct("Coconut Water","coconut-water","Pure coconut water 1L",drinkCat.Id,v4.Id,5.50m,"SGD",500,"Singapore");
        AddProduct("Qahwa Arabic Coffee","qahwa","Arabic coffee cardamom 250g",drinkCat.Id,v5.Id,35.00m,"AED",80,"UAE");
        AddProduct("Rose Water Drink","rose-water","Rose water beverage 750ml",drinkCat.Id,v5.Id,18.00m,"AED",120,"UAE");
        AddProduct("Bandung Concentrate","bandung","Rose milk mix",drinkCat.Id,v2.Id,10.00m,"MYR",200,"Malaysia");
        // Meat
        AddProduct("Ayam Kampung","ayam-kampung","Village chicken 1.5kg",meatCat.Id,v1.Id,28.00m,"MYR",100,"Malaysia");
        AddProduct("Daging Kambing","daging-kambing","Goat meat 500g",meatCat.Id,v2.Id,42.00m,"MYR",80,"Malaysia");
        AddProduct("Sate Ayam","sate-ayam","Chicken satay 10pcs",meatCat.Id,v1.Id,18.00m,"MYR",200,"Malaysia");
        AddProduct("Rendang Daging","rendang-daging","Beef rendang 400g",meatCat.Id,v2.Id,25.00m,"MYR",150,"Malaysia");
        AddProduct("Chicken Nugget","chicken-nugget","Halal nuggets 500g",meatCat.Id,v4.Id,8.50m,"SGD",300,"Singapore");
        AddProduct("Lamb Mince","lamb-mince","Halal lamb mince 400g",meatCat.Id,v5.Id,45.00m,"AED",60,"UAE");
        AddProduct("Sosis Sapi","sosis-sapi","Beef sausage 10pcs",meatCat.Id,v3.Id,35000m,"IDR",250,"Indonesia");
        AddProduct("Otak-Otak","otak-otak","Grilled fish cake 12pcs",meatCat.Id,v1.Id,16.00m,"MYR",180,"Malaysia");
        // Spices
        AddProduct("Rendang Paste","rendang-paste","Rendang spice paste 200g",spiceCat.Id,v1.Id,12.00m,"MYR",400,"Malaysia");
        AddProduct("Sambal Terasi","sambal-terasi","Chili sauce 250ml",spiceCat.Id,v3.Id,18000m,"IDR",300,"Indonesia");
        AddProduct("Kari Ayam Powder","kari-ayam","Curry powder 100g",spiceCat.Id,v2.Id,7.50m,"MYR",500,"Malaysia");
        AddProduct("Turmeric Ground","turmeric","Organic turmeric 150g",spiceCat.Id,v4.Id,6.00m,"SGD",250,"Singapore");
        AddProduct("Zaatar Blend","zaatar","Herb blend 120g",spiceCat.Id,v5.Id,22.00m,"AED",180,"UAE");
        AddProduct("Black Pepper","black-pepper","Ground black pepper 100g",spiceCat.Id,v1.Id,9.00m,"MYR",600,"Malaysia");
        AddProduct("Curry Leaf","curry-leaf","Dried curry leaves 30g",spiceCat.Id,v4.Id,4.50m,"SGD",400,"Singapore");
        AddProduct("Bumbu Nasi Goreng","bumbu-nasi","Fried rice paste 150g",spiceCat.Id,v3.Id,12000m,"IDR",350,"Indonesia");
        // Bakery
        AddProduct("Roti Canai","roti-canai","Frozen roti canai 8pcs",bakeryCat.Id,v1.Id,10.00m,"MYR",200,"Malaysia");
        AddProduct("Naan Garlic","naan-garlic","Garlic butter naan 6pcs",bakeryCat.Id,v2.Id,11.00m,"MYR",150,"Malaysia");
        AddProduct("Wholemeal Bread","wholemeal-bread","Wholemeal bread 400g",bakeryCat.Id,v4.Id,4.20m,"SGD",300,"Singapore");
        AddProduct("Kuih Bahulu","kuih-bahulu","Malay sponge cake 12pcs",bakeryCat.Id,v1.Id,13.00m,"MYR",100,"Malaysia");
        AddProduct("Blueberry Muffin","muffin","Blueberry muffins 4pcs",bakeryCat.Id,v4.Id,7.50m,"SGD",180,"Singapore");
        AddProduct("Date Cookies","date-cookies","Date cookies 250g",bakeryCat.Id,v5.Id,25.00m,"AED",90,"UAE");
        AddProduct("Pia Durian","pia-durian","Durian pia 6pcs",bakeryCat.Id,v1.Id,18.00m,"MYR",120,"Malaysia");
        AddProduct("Martabak Manis","martabak","Sweet pancake frozen",bakeryCat.Id,v3.Id,25000m,"IDR",80,"Indonesia");
        // Skincare
        AddProduct("Vitamin C Serum","vitamin-c","Halal Vitamin C 30ml",skinCat.Id,v4.Id,32.00m,"SGD",200,"Singapore");
        AddProduct("Spirulina Mask","spirulina-mask","Face mask 10 sheets",skinCat.Id,v3.Id,45000m,"IDR",150,"Indonesia");
        AddProduct("Aloe Moisturizer","aloe-moisturizer","Aloe vera cream 100ml",skinCat.Id,v2.Id,22.00m,"MYR",250,"Malaysia");
        AddProduct("Rose Toner","rose-toner","Rose water toner 200ml",skinCat.Id,v5.Id,28.00m,"AED",180,"UAE");
        // Haircare
        AddProduct("Palm Shampoo","palm-shampoo","Palm oil shampoo 400ml",hairCat.Id,v1.Id,15.00m,"MYR",300,"Malaysia");
        AddProduct("Argan Hair Oil","argan-oil","Argan oil treatment 100ml",hairCat.Id,v5.Id,35.00m,"AED",120,"UAE");
        AddProduct("Hijab Conditioner","hijab-cond","Lightweight conditioner 300ml",hairCat.Id,v2.Id,18.00m,"MYR",200,"Malaysia");
        // Supplements
        AddProduct("Tualang Honey","tualang-honey","Forest honey 500ml",suppCat.Id,v1.Id,45.00m,"MYR",100,"Malaysia");
        AddProduct("Habbatus Sauda","habbatus-sauda","Black seed capsules 60pcs",suppCat.Id,v5.Id,55.00m,"AED",150,"UAE");
        AddProduct("Medjool Dates","medjool-dates","Premium dates 1kg",suppCat.Id,v5.Id,65.00m,"AED",200,"UAE");
        modelBuilder.Entity<Product>().HasData(products);

        // Certificates + Verifications
        var certs = new List<Certificate>();
        var evidence = new List<VerificationEvidence>();
        var verifications = new List<HalalVerification>();
        int certIdx = 0;
        foreach (var p in products.Where((_, i) => i % 2 == 0).Take(25))
        {
            certIdx++;
            var certId = Guid.Parse($"C{certIdx:D3}0000-0000-0000-0000-000000000000");
            var verId = Guid.Parse($"D{certIdx:D3}0000-0000-0000-0000-000000000000");
            var isVerified = certIdx <= 18;
            certs.Add(new Certificate { Id = certId, ProductId = p.Id, CertificateNumber = $"JAKIM-{2024 + (certIdx % 3)}-{certIdx:D4}", CertificationBody = p.VendorId == v3.Id ? "MUI" : p.VendorId == v5.Id ? "ESMA" : "JAKIM", Jurisdiction = p.VendorId == v3.Id ? "ID" : p.VendorId == v5.Id ? "AE" : "MY", Status = isVerified ? CertificateStatus.Verified : CertificateStatus.DocumentReview, IssueDate = now.AddDays(-365), ExpiryDate = now.AddDays(certIdx > 22 ? -30 : certIdx > 20 ? 15 : 365), Scope = $"Certification for {p.Title}", CreatedAt = now.AddDays(-300) });
            verifications.Add(new HalalVerification { Id = verId, ProductId = p.Id, ComplianceStatus = isVerified ? ComplianceStatus.Verified : ComplianceStatus.ManualReview, PolicyVersion = "MY-v3", Jurisdiction = p.VendorId == v3.Id ? "ID" : p.VendorId == v5.Id ? "AE" : "MY", ReasonCodes = isVerified ? [] : ["CERT_UNDER_REVIEW"], MissingEvidence = isVerified ? [] : ["supplier_declaration"], RequiresHumanReview = !isVerified, VerifiedAt = now.AddDays(-250) });
            evidence.Add(new VerificationEvidence { Id = Guid.Parse($"E{certIdx:D3}0000-0000-0000-0000-000000000001"), VerificationId = verId, EvidenceType = "certificate", Source = "Cert body database", Confidence = isVerified ? 0.95 : 0.4, CollectedAt = now.AddDays(-260) });
            evidence.Add(new VerificationEvidence { Id = Guid.Parse($"E{certIdx:D3}0000-0000-0000-0000-000000000002"), VerificationId = verId, EvidenceType = "ingredient", Source = "Label analysis", Confidence = isVerified ? 0.88 : 0.55, CollectedAt = now.AddDays(-258) });
        }
        modelBuilder.Entity<Certificate>().HasData(certs);
        modelBuilder.Entity<HalalVerification>().HasData(verifications);
        modelBuilder.Entity<VerificationEvidence>().HasData(evidence);
    }
}
