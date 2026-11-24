namespace BizSpace.Data;

internal static class DemoCatalogImages
{
    public static IReadOnlyDictionary<string, IReadOnlyList<SeedImage>> ProductImages { get; } =
        new Dictionary<string, IReadOnlyList<SeedImage>>(StringComparer.OrdinalIgnoreCase)
        {
            ["JBL Go 4 Portable Bluetooth Speaker"] =
            [
                new("bizspace-demo/products/jbl-speaker-01", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204545/bizspace-demo/products/jbl-speaker-01.jpg", "jpg", 1600, 1067, 0, "https://unsplash.com/photos/a-bluetooth-speaker-sitting-on-top-of-a-table-lXFuL37cMI8", "DP Singh Bhullar"),
                new("bizspace-demo/products/jbl-speaker-02", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204546/bizspace-demo/products/jbl-speaker-02.jpg", "jpg", 1600, 1200, 1, "https://unsplash.com/photos/black-and-silver-portable-speaker-S0B-pmGjdVA", "Habib Dadkhah")
            ],
            ["Tempo Active Smartwatch"] =
            [
                new("bizspace-demo/products/tempo-smartwatch-01", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204548/bizspace-demo/products/tempo-smartwatch-01.jpg", "jpg", 1600, 900, 0, "https://unsplash.com/photos/smartwatch-on-a-persons-wrist-at-night-NAZc20WnrII", "Amanz"),
                new("bizspace-demo/products/tempo-smartwatch-02", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204549/bizspace-demo/products/tempo-smartwatch-02.jpg", "jpg", 1600, 2000, 1, "https://unsplash.com/photos/a-person-in-a-green-sweater-with-a-smart-watch-on-their-wrist-h0nH78psQ74", "Marcin Kempa")
            ],
            ["EchoBud True Wireless Earbuds"] =
            [
                new("bizspace-demo/products/echobud-earbuds-01", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204551/bizspace-demo/products/echobud-earbuds-01.jpg", "jpg", 1600, 1067, 0, "https://unsplash.com/photos/white-wireless-earbuds-and-charging-case-on-dark-background-rN1hLIGV4gE", "atelierbyvineeth"),
                new("bizspace-demo/products/echobud-earbuds-02", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204552/bizspace-demo/products/echobud-earbuds-02.jpg", "jpg", 1600, 1200, 1, "https://unsplash.com/photos/white-wireless-earbuds-with-charging-case-on-gray-background-clrTR7gDRho", "Andrey Matveev")
            ],
            ["Terra Steel Travel Tumbler"] =
            [
                new("bizspace-demo/products/terra-tumbler-01", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204554/bizspace-demo/products/terra-tumbler-01.jpg", "jpg", 1600, 2487, 0, "https://unsplash.com/photos/stainless-steel-travel-mug-with-join-the-coffee-community-text-j1CBL_jY3uY", "Milos Lopusina"),
                new("bizspace-demo/products/terra-tumbler-02", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204571/bizspace-demo/products/terra-tumbler-02.jpg", "jpg", 1600, 2400, 1, "https://unsplash.com/photos/a-person-holding-a-coffee-cup-in-their-hand-nNjuUJ6jOHA", "Natilyn Hicks Photography")
            ],
            ["Luma Arc Desk Lamp"] =
            [
                new("bizspace-demo/products/luma-lamp-01", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204572/bizspace-demo/products/luma-lamp-01.jpg", "jpg", 1600, 1066, 0, "https://unsplash.com/photos/a-desk-lamp-illuminates-a-cluttered-workspace-at-night-qxmw0791V5Y", "Haewon Oh"),
                new("bizspace-demo/products/luma-lamp-02", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204575/bizspace-demo/products/luma-lamp-02.jpg", "jpg", 1600, 681, 1, "https://unsplash.com/photos/row-of-identical-desk-lamps-against-a-wall-bwU3Uc4e7RI", "YiChuan Li")
            ],
            ["BrewCraft Pour-Over Coffee Set"] =
            [
                new("bizspace-demo/products/brewcraft-pour-over-01", "/images/products/brewcraft-pour-over-01.jpg", "jpg", 1600, 1067, 0, "https://unsplash.com/photos/coffee-brewing-equipment-on-a-counter-q0qXYST9iKY", "Yasin Onus"),
                new("bizspace-demo/products/brewcraft-pour-over-02", "/images/products/brewcraft-pour-over-02.jpg", "jpg", 1600, 2400, 1, "https://unsplash.com/photos/clear-glass-coffee-pour-over-dripper-and-server-casting-shadows-etL0cJaacYo", "Madeline Liu")
            ],
            ["Everyday Canvas Tote"] =
            [
                new("bizspace-demo/products/canvas-tote-01", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204579/bizspace-demo/products/canvas-tote-01.jpg", "jpg", 1600, 2133, 0, "https://unsplash.com/photos/a-canvas-tote-bag-hangs-on-a-pole-WR4Ev2iSMt4", "JIN Qin"),
                new("bizspace-demo/products/canvas-tote-02", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204583/bizspace-demo/products/canvas-tote-02.jpg", "jpg", 1600, 2400, 1, "https://unsplash.com/photos/tote-bags-on-green-door-krakow-GRPyRkwUIHI", "Anton Tseiko")
            ],
            ["Cloudstep White Sneakers"] =
            [
                new("bizspace-demo/products/cloudstep-sneakers-01", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204586/bizspace-demo/products/cloudstep-sneakers-01.jpg", "jpg", 1600, 2400, 0, "https://unsplash.com/photos/sneakers-on-display-stands-under-warm-focused-lighting-DXTT02-ee-8", "Sou Jest"),
                new("bizspace-demo/products/cloudstep-sneakers-02", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204587/bizspace-demo/products/cloudstep-sneakers-02.jpg", "jpg", 1600, 2000, 1, "https://unsplash.com/photos/man-wearing-white-low-top-sneakers-0Mfukiqo4Us", "Aman Jakhar")
            ],
            ["Classic Leather Wallet"] =
            [
                new("bizspace-demo/products/northline-wallet-01", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204592/bizspace-demo/products/northline-wallet-01.jpg", "jpg", 1600, 2000, 0, "https://unsplash.com/photos/a-brown-leather-wallet-sitting-on-top-of-a-table-G9agP2u0Z6A", "nimmafotugrapher"),
                new("bizspace-demo/products/northline-wallet-02", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204595/bizspace-demo/products/northline-wallet-02.jpg", "jpg", 1600, 2133, 1, "https://unsplash.com/photos/a-black-leather-wallet-on-a-wooden-surface-KRePHiXsnnc", "Alexey Demidov")
            ],
            ["C-Glow Vitamin C Serum"] =
            [
                new("bizspace-demo/products/cglow-serum-01", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204597/bizspace-demo/products/cglow-serum-01.jpg", "jpg", 1600, 2400, 0, "https://unsplash.com/photos/skincare-serum-bottle-with-box-and-liquid-sample-IwnZta9eVVU", "The Good Hygiene Co"),
                new("bizspace-demo/products/cglow-serum-02", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204599/bizspace-demo/products/cglow-serum-02.jpg", "jpg", 1600, 2399, 1, "https://unsplash.com/photos/skincare-serum-bottle-sits-on-a-seashell-jYifNBo1j4Y", "Tamara Harhai")
            ],
            ["Dewdrop Face Moisturizer"] =
            [
                new("bizspace-demo/products/dewdrop-moisturizer-01", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204601/bizspace-demo/products/dewdrop-moisturizer-01.jpg", "jpg", 1600, 1067, 0, "https://unsplash.com/photos/a-jar-of-cream-sitting-on-top-of-a-white-pedestal-DIy29_zBgQk", "Ira Kuziv"),
                new("bizspace-demo/products/dewdrop-moisturizer-02", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204603/bizspace-demo/products/dewdrop-moisturizer-02.jpg", "jpg", 1600, 2138, 1, "https://unsplash.com/photos/a-close-up-of-a-jar-of-cream-WcRaXxOH10w", "Edz Norton")
            ]
        };

    public static IReadOnlyDictionary<string, SeedImage> CategoryImages { get; } =
        new Dictionary<string, SeedImage>(StringComparer.OrdinalIgnoreCase)
        {
            ["Electronics & Accessories"] = new("bizspace-demo/categories/electronics-and-accessories", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204605/bizspace-demo/categories/electronics-and-accessories.svg", "svg", 24, 24, 0, "https://github.com/lucide-icons/lucide/blob/main/icons/headphones.svg", "Lucide Icons (ISC)"),
            ["Books & Stationery"] = new("bizspace-demo/categories/books-and-stationery", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204607/bizspace-demo/categories/books-and-stationery.svg", "svg", 24, 24, 0, "https://github.com/lucide-icons/lucide/blob/main/icons/book-open.svg", "Lucide Icons (ISC)"),
            ["Home & Kitchen"] = new("bizspace-demo/categories/home-and-kitchen", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204610/bizspace-demo/categories/home-and-kitchen.svg", "svg", 24, 24, 0, "https://github.com/lucide-icons/lucide/blob/main/icons/house.svg", "Lucide Icons (ISC)"),
            ["Fashion & Accessories"] = new("bizspace-demo/categories/fashion-and-accessories", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204612/bizspace-demo/categories/fashion-and-accessories.svg", "svg", 24, 24, 0, "https://github.com/lucide-icons/lucide/blob/main/icons/shirt.svg", "Lucide Icons (ISC)"),
            ["Beauty & Personal Care"] = new("bizspace-demo/categories/beauty-and-personal-care", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204614/bizspace-demo/categories/beauty-and-personal-care.svg", "svg", 24, 24, 0, "https://github.com/lucide-icons/lucide/blob/main/icons/sparkles.svg", "Lucide Icons (ISC)"),
            ["Sports & Outdoors"] = new("bizspace-demo/categories/sports-and-outdoors", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204616/bizspace-demo/categories/sports-and-outdoors.svg", "svg", 24, 24, 0, "https://github.com/lucide-icons/lucide/blob/main/icons/dumbbell.svg", "Lucide Icons (ISC)"),
            ["Toys & Baby"] = new("bizspace-demo/categories/toys-and-baby", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204618/bizspace-demo/categories/toys-and-baby.svg", "svg", 24, 24, 0, "https://github.com/lucide-icons/lucide/blob/main/icons/baby.svg", "Lucide Icons (ISC)"),
            ["Grocery & Gourmet"] = new("bizspace-demo/categories/grocery-and-gourmet", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204621/bizspace-demo/categories/grocery-and-gourmet.svg", "svg", 24, 24, 0, "https://github.com/lucide-icons/lucide/blob/main/icons/shopping-basket.svg", "Lucide Icons (ISC)"),
            ["Pet Supplies"] = new("bizspace-demo/categories/pet-supplies", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204623/bizspace-demo/categories/pet-supplies.svg", "svg", 24, 24, 0, "https://github.com/lucide-icons/lucide/blob/main/icons/paw-print.svg", "Lucide Icons (ISC)"),
            ["Automotive"] = new("bizspace-demo/categories/automotive", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204625/bizspace-demo/categories/automotive.svg", "svg", 24, 24, 0, "https://github.com/lucide-icons/lucide/blob/main/icons/car.svg", "Lucide Icons (ISC)"),
            ["Garden & Outdoor Living"] = new("bizspace-demo/categories/garden-and-outdoor-living", "https://res.cloudinary.com/s1kfhtvi/image/upload/v1795204627/bizspace-demo/categories/garden-and-outdoor-living.svg", "svg", 24, 24, 0, "https://github.com/lucide-icons/lucide/blob/main/icons/flower-2.svg", "Lucide Icons (ISC)")
        };
}

internal sealed record SeedImage(
    string PublicId,
    string SecureUrl,
    string Format,
    int? Width,
    int? Height,
    int SortOrder,
    string SourcePageUrl,
    string Credit);
