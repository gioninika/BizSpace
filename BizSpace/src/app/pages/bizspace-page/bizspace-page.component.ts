import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter, firstValueFrom } from 'rxjs';
import { ApiService } from '../../services/api.service';
import {
  AppStateService,
  CategoryDto,
  MarketingCampaignDto,
  OrderDto,
  OrderLineDto,
  ProductDto,
  ProductImageDto,
  ProductReviewDto,
  ProductView,
  SavedAddressDto,
  SavedAddressWriteRequest,
  UserDto,
  WorkspaceSection,
} from '../../services/app-state.service';
import {
  AuthResponse,
  AuthSessionService,
  WorkspaceRole,
} from '../../services/auth-session.service';
import { HeaderComponent } from '../../components/header/header.component';
import { FooterComponent } from '../../components/footer/footer.component';

type AuthMode = 'login' | 'register' | 'verify' | 'forgot' | 'reset';
type ManagerProductReview = ProductReviewDto & { productName: string; storeName: string };

@Component({
  selector: 'app-bizspace-page',
  templateUrl: './bizspace-page.component.html',
  standalone: true,
  imports: [HeaderComponent, FooterComponent, RouterOutlet],
  styleUrl: './bizspace-page.component.scss',
})
export class BizSpacePageComponent implements OnInit {
  protected readonly state = inject(AppStateService);
  protected readonly auth = inject(AuthSessionService);
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private applicationReady = false;
  private loadedReviewsForProductId: number | null = null;
  private managerReviewRequestId = 0;

  protected readonly authOpen = signal(false);
  protected readonly authMode = signal<AuthMode>('login');
  protected readonly authBusy = signal(false);
  protected readonly authError = signal('');
  protected readonly pendingEmail = signal('');
  protected readonly managerRegistration = signal(false);
  protected readonly productModalOpen = signal(false);
  protected readonly editingProduct = signal<ProductView | null>(null);
  protected readonly storeModalOpen = signal(false);
  protected readonly campaignModalOpen = signal(false);
  protected readonly editingCampaign = signal<MarketingCampaignDto | null>(null);
  protected readonly campaignTarget = signal<'store' | 'category' | 'products'>('store');
  protected readonly campaignStoreId = signal<number | null>(null);
  protected readonly campaignProductIds = signal<ReadonlySet<number>>(new Set<number>());
  protected readonly campaignCategories = computed(() => {
    const storeId = this.campaignStoreId();
    if (!storeId) return [];
    const categoryIds = new Set(
      this.state.productViews()
        .filter((product) => product.storeId === storeId)
        .map((product) => product.categoryId),
    );
    return this.state.allCategories().filter((category) => categoryIds.has(category.id));
  });
  protected readonly categoryModalOpen = signal(false);
  protected readonly editingCategory = signal<CategoryDto | null>(null);
  protected readonly profileBusy = signal(false);
  protected readonly profileError = signal('');
  protected readonly passwordBusy = signal(false);
  protected readonly passwordError = signal('');
  protected readonly addressEditorOpen = signal(false);
  protected readonly addressBusy = signal(false);
  protected readonly editingAddress = signal<SavedAddressDto | null>(null);
  protected readonly checkoutMode = signal(false);
  protected readonly paymentMethod = signal<'Card' | 'Cash'>('Cash');
  protected readonly deliveryMethod = signal<'Pickup' | 'Courier'>('Courier');
  protected readonly selectedAddressId = signal<number | null>(null);
  protected readonly selectedProductImageUrl = signal<string | null>(null);
  protected readonly productPhotoBusy = signal(false);
  protected readonly productReviews = signal<ProductReviewDto[]>([]);
  protected readonly managerReviews = signal<ManagerProductReview[]>([]);
  protected readonly reviewsLoading = signal(false);
  protected readonly reviewsError = signal('');
  protected readonly managerReviewsLoading = signal(false);
  protected readonly managerReviewsError = signal('');
  protected readonly customerReviewStarFilter = signal<number | null>(null);
  protected readonly managerReviewStarFilter = signal<number | null>(null);
  protected readonly managerReviewProductId = signal<number | null>(null);
  protected readonly starOptions = [5, 4, 3, 2, 1];
  protected readonly reviewListOpen = signal(false);
  protected readonly reviewFormOpen = signal(false);
  protected readonly reviewSubmitting = signal(false);
  protected readonly reviewFormError = signal('');
  protected readonly expandedOrderIds = signal<ReadonlySet<number>>(new Set());
  protected readonly contactFeedback = signal('');
  protected readonly managerNav = ['Overview', 'Products', 'Orders', 'Campaigns', 'Reviews'];
  protected readonly adminNav = [
    'Overview',
    'Products',
    'Stores',
    'Customers',
    'Categories',
    'Orders',
  ];
  protected readonly activeCustomerHeading = computed(() => {
    const section = this.state.section();
    if (section === 'Shop') return 'Products';
    if (section === 'My orders') return 'My orders';
    if (section === 'Wishlist') return 'Wishlist';
    return 'Find your kind of good.';
  });
  protected readonly adminRevenueOrders = computed(() =>
    this.state
      .orders()
      .filter((order) => ['PAID', 'SHIPPED', 'DELIVERED'].includes(order.status.toUpperCase())),
  );
  protected readonly adminOrdersTotal = computed(() =>
    this.adminRevenueOrders().reduce((sum, order) => sum + order.totalAmount, 0),
  );
  protected readonly adminRevenueOrderCount = computed(() => this.adminRevenueOrders().length);
  protected readonly filteredProductReviews = computed(() => {
    const rating = this.customerReviewStarFilter();
    return this.productReviews().filter((review) => rating === null || review.rating === rating);
  });
  protected readonly filteredManagerReviews = computed(() => {
    const productId = this.managerReviewProductId();
    const rating = this.managerReviewStarFilter();
    return this.managerReviews().filter(
      (review) =>
        (productId === null || review.productId === productId) &&
        (rating === null || review.rating === rating),
    );
  });

  ngOnInit(): void {
    if (typeof window === 'undefined') return;
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => {
        if (this.applicationReady) this.syncSectionFromRoute();
      });
    void this.state.initialize().then(() => {
      this.applicationReady = true;
      this.syncSectionFromRoute();
    });
  }

  private syncSectionFromRoute(): void {
    const segments = this.router.url.split(/[?#]/, 1)[0].split('/').filter(Boolean);
    const role = this.state.role();
    if (segments[0] === 'product' && Number(segments[1])) {
      if (role === 'StoreManager' || role === 'Admin') {
        void this.router.navigateByUrl(this.pathForSection(this.state.section()), {
          replaceUrl: true,
        });
        return;
      }
      const productId = Number(segments[1]);
      if (this.loadedReviewsForProductId !== productId) {
        this.productReviews.set([]);
        this.reviewFormOpen.set(false);
        this.customerReviewStarFilter.set(null);
        this.loadedReviewsForProductId = null;
      }
      this.state.selectedProductId.set(productId);
      this.reviewListOpen.set(segments[2] === 'reviews');
      void this.loadProductReviews(productId);
      return;
    }
    this.reviewListOpen.set(false);
    this.reviewFormOpen.set(false);

    const customerSections: Record<string, WorkspaceSection> = {
      discover: 'Discover',
      products: 'Shop',
      stores: 'Stores',
      'my-orders': 'My orders',
      wishlist: 'Wishlist',
      profile: 'Profile',
      'address-book': 'Address book',
      about: 'About us',
      contact: 'Contact us',
    };
    const managerSections: Record<string, WorkspaceSection> = {
      overview: 'Overview',
      products: 'Products',
      orders: 'Orders',
      campaigns: 'Campaigns',
      reviews: 'Reviews',
    };
    const adminSections: Record<string, WorkspaceSection> = {
      overview: 'Overview',
      products: 'Products',
      stores: 'Stores',
      customers: 'Customers',
      categories: 'Categories',
      orders: 'Orders',
    };

    if (role === 'StoreManager') {
      if (segments[0] !== 'manager' || !managerSections[segments[1]]) {
        void this.router.navigateByUrl(this.pathForSection('Overview'), { replaceUrl: true });
        return;
      }
      this.state.setSection(managerSections[segments[1]]);
      if (managerSections[segments[1]] === 'Reviews') void this.loadManagerReviews();
      return;
    }
    if (role === 'Admin') {
      if (segments[0] !== 'admin' || !adminSections[segments[1]]) {
        void this.router.navigateByUrl(this.pathForSection('Overview'), { replaceUrl: true });
        return;
      }
      this.state.setSection(adminSections[segments[1]]);
      return;
    }

    if (segments[0] === 'stores' && Number(segments[1]))
      this.state.customerStoreId.set(Number(segments[1]));
    else this.state.customerStoreId.set(null);
    const section = customerSections[segments[0]];
    if (section) this.state.setSection(section);
    else if (segments[0] !== 'product')
      void this.router.navigateByUrl('/discover', { replaceUrl: true });
  }

  private pathForSection(section: string): string {
    const role = this.state.role();
    const slug = section.toLowerCase().replaceAll(' ', '-');
    if (role === 'StoreManager') return `/manager/${slug}`;
    if (role === 'Admin') return `/admin/${slug}`;
    const customerPath: Record<string, string> = {
      discover: 'discover',
      shop: 'products',
      stores: 'stores',
      'my-orders': 'my-orders',
      wishlist: 'wishlist',
      profile: 'profile',
      'address-book': 'address-book',
      'about-us': 'about',
      'contact-us': 'contact',
    };
    return `/${customerPath[slug] ?? 'discover'}`;
  }

  protected navItems(): string[] {
    if (this.state.role() === 'StoreManager') return this.managerNav;
    if (this.state.role() === 'Admin') return this.adminNav;
    return [];
  }
  protected chooseSection(section: string): void {
    this.state.customerStoreId.set(null);
    this.state.selectedProductId.set(null);
    this.state.setSection(section as WorkspaceSection);
    void this.router.navigateByUrl(this.pathForSection(section));
  }
  protected openCustomerStore(storeId: number): void {
    this.state.setCategory(0);
    this.state.setSearch('');
    this.state.selectedProductId.set(null);
    this.state.customerStoreId.set(storeId);
    this.state.setSection('Stores');
    void this.router.navigateByUrl(`/stores/${storeId}`);
  }
  protected openProduct(productId: number): void {
    const product = this.state.productViews().find((item) => item.id === productId);
    this.selectedProductImageUrl.set(
      product?.images.slice().sort((a, b) => a.sortOrder - b.sortOrder)[0]?.secureUrl ?? null,
    );
    this.state.selectedProductId.set(productId);
    void this.router.navigateByUrl(`/product/${productId}`);
    if (typeof window !== 'undefined') window.scrollTo({ top: 0, behavior: 'smooth' });
  }
  protected selectProductImage(url: string): void {
    this.selectedProductImageUrl.set(url);
  }
  protected closeProduct(): void {
    this.state.selectedProductId.set(null);
    this.selectedProductImageUrl.set(null);
    void this.router.navigateByUrl(
      this.state.customerStoreId()
        ? `/stores/${this.state.customerStoreId()}`
        : this.pathForSection(this.state.section()),
    );
    if (typeof window !== 'undefined') window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  protected openProductReviews(productId: number): void {
    void this.router.navigateByUrl(`/product/${productId}/reviews`);
    if (typeof window !== 'undefined') window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  protected returnToProduct(productId: number): void {
    void this.router.navigateByUrl(`/product/${productId}`);
    if (typeof window !== 'undefined') window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  protected async writeReviewFromList(productId: number): Promise<void> {
    if (!this.auth.isAuthenticated()) {
      this.openAuth('login');
      return;
    }
    if (this.auth.role() !== 'Customer' || this.currentUserHasReviewed()) return;
    await this.router.navigateByUrl(`/product/${productId}`);
    this.reviewListOpen.set(false);
    this.reviewFormError.set('');
    this.reviewFormOpen.set(true);
    if (typeof window !== 'undefined') window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  protected openReviewForm(): void {
    if (!this.auth.isAuthenticated()) {
      this.openAuth('login');
      return;
    }
    if (this.auth.role() !== 'Customer' || this.currentUserHasReviewed()) return;
    this.reviewFormError.set('');
    this.reviewFormOpen.set(true);
  }

  protected currentUserHasReviewed(): boolean {
    const userId = this.auth.userId();
    return userId !== null && this.productReviews().some((review) => review.userId === userId);
  }

  protected reviewStars(rating: number): string {
    return '★'.repeat(rating).padEnd(5, '☆');
  }

  private async loadProductReviews(productId: number): Promise<void> {
    if (this.loadedReviewsForProductId === productId) return;
    this.loadedReviewsForProductId = productId;
    this.reviewsLoading.set(true);
    this.reviewsError.set('');
    try {
      this.productReviews.set(
        (await firstValueFrom(this.api.get<ProductReviewDto[]>(`/products/${productId}/reviews`))) ?? [],
      );
    } catch (error) {
      this.loadedReviewsForProductId = null;
      this.reviewsError.set(error instanceof Error ? error.message : 'Reviews could not be loaded.');
    } finally {
      this.reviewsLoading.set(false);
    }
  }

  protected retryProductReviews(productId: number): void {
    this.loadedReviewsForProductId = null;
    void this.loadProductReviews(productId);
  }

  protected setCustomerReviewStarFilter(value: string): void {
    this.customerReviewStarFilter.set(value ? Number(value) : null);
  }

  protected setManagerReviewStarFilter(value: string): void {
    this.managerReviewStarFilter.set(value ? Number(value) : null);
  }

  protected setManagerReviewProductFilter(value: string): void {
    this.managerReviewProductId.set(value ? Number(value) : null);
  }

  protected async loadManagerReviews(): Promise<void> {
    const requestId = ++this.managerReviewRequestId;
    const products = this.state.productViews();
    this.managerReviewsLoading.set(true);
    this.managerReviewsError.set('');
    try {
      if (!products.length) {
        this.managerReviews.set([]);
        return;
      }
      const reviewGroups = await Promise.all(
        products.map(async (product) => {
          const reviews =
            (await firstValueFrom(
              this.api.get<ProductReviewDto[]>(`/products/${product.id}/reviews`),
            )) ?? [];
          return reviews.map((review) => ({
            ...review,
            productName: product.name,
            storeName: product.storeName,
          }));
        }),
      );
      if (requestId !== this.managerReviewRequestId) return;
      this.managerReviews.set(
        reviewGroups
          .flat()
          .sort(
            (first, second) =>
              new Date(second.createdAtUtc).getTime() - new Date(first.createdAtUtc).getTime(),
          ),
      );
    } catch (error) {
      if (requestId !== this.managerReviewRequestId) return;
      this.managerReviewsError.set(
        error instanceof Error ? error.message : 'Reviews could not be loaded.',
      );
    } finally {
      if (requestId === this.managerReviewRequestId) this.managerReviewsLoading.set(false);
    }
  }

  protected async submitProductReview(productId: number, form: HTMLFormElement): Promise<void> {
    if (!this.auth.isAuthenticated() || this.auth.role() !== 'Customer') {
      this.openAuth('login');
      return;
    }
    if (this.currentUserHasReviewed()) {
      this.reviewFormError.set('You have already reviewed this product.');
      return;
    }

    const data = new FormData(form);
    const rating = Number(data.get('rating'));
    const comment = String(data.get('comment') ?? '').trim();
    if (!Number.isInteger(rating) || rating < 1 || rating > 5) {
      this.reviewFormError.set('Choose a rating from 1 to 5 stars.');
      return;
    }
    if (comment.length > 2000) {
      this.reviewFormError.set('Your review must be 2,000 characters or fewer.');
      return;
    }

    this.reviewSubmitting.set(true);
    this.reviewFormError.set('');
    try {
      const created = await firstValueFrom(
        this.api.post<ProductReviewDto>(`/products/${productId}/reviews`, { rating, comment }),
      );
      this.productReviews.update((reviews) => [...reviews, created]);
      this.state.products.update((products) =>
        products.map((product) => {
          if (product.id !== productId) return product;
          const nextReviewCount = product.reviewCount + 1;
          return {
            ...product,
            reviewCount: nextReviewCount,
            averageRating:
              (product.averageRating * product.reviewCount + created.rating) / nextReviewCount,
          };
        }),
      );
      this.reviewsError.set('');
      this.reviewFormOpen.set(false);
      form.reset();
      this.state.notify('Thanks for sharing your review.');
    } catch (error) {
      this.reviewFormError.set(
        error instanceof Error ? error.message : 'Your review could not be submitted.',
      );
    } finally {
      this.reviewSubmitting.set(false);
    }
  }

  protected orderedProductImages(product: ProductView): ProductImageDto[] {
    return product.images.slice().sort((a, b) => a.sortOrder - b.sortOrder);
  }
  protected setDeliveryMethod(method: 'Pickup' | 'Courier'): void {
    this.deliveryMethod.set(method);
    if (method === 'Pickup') this.selectedAddressId.set(null);
  }
  protected isNavActive(section: string): boolean {
    return this.state.section() === section;
  }
  protected prettyRole(role: WorkspaceRole | null): string {
    return role === 'StoreManager' ? 'Store manager' : (role ?? 'Customer');
  }
  protected formatMoney(value: number): string {
    const currency =
      typeof window !== 'undefined' ? window.bizSpaceConfig?.currency || 'USD' : 'USD';
    return new Intl.NumberFormat(undefined, { style: 'currency', currency }).format(value);
  }
  protected formatDate(value: string): string {
    return value ? new Date(value).toLocaleDateString() : '—';
  }
  protected contactEmail(): string {
    const email =
      typeof window !== 'undefined' ? (window.bizSpaceConfig?.contactEmail?.trim() ?? '') : '';
    return /^[^\s@<>]+@[^\s@<>]+$/.test(email) ? email : '';
  }
  protected contactPhone(): string {
    return typeof window !== 'undefined' ? (window.bizSpaceConfig?.contactPhone?.trim() ?? '') : '';
  }
  protected contactPhoneHref(): string {
    return `tel:${this.contactPhone().replace(/[^\d+]/g, '')}`;
  }
  protected facebookUrl(): string {
    return this.contactSocialUrl('facebookUrl');
  }
  protected instagramUrl(): string {
    return this.contactSocialUrl('instagramUrl');
  }
  protected hasContactDetails(): boolean {
    return Boolean(this.contactPhone() || this.facebookUrl() || this.instagramUrl());
  }
  private contactSocialUrl(key: 'facebookUrl' | 'instagramUrl'): string {
    const value = typeof window !== 'undefined' ? (window.bizSpaceConfig?.[key]?.trim() ?? '') : '';
    try {
      const url = new URL(value);
      return url.protocol === 'https:' ? url.href : '';
    } catch {
      return '';
    }
  }
  protected submitContactMessage(form: HTMLFormElement): void {
    this.openContactMessage(form, 'default');
  }
  protected openContactMessage(
    form: HTMLFormElement,
    provider: 'gmail' | 'outlook' | 'default',
  ): void {
    if (!form.reportValidity()) return;
    const recipient = this.contactEmail();
    if (!recipient || typeof window === 'undefined') {
      this.contactFeedback.set(
        'Contact email is not configured. Set contactEmail in public/bizspace-config.js.',
      );
      return;
    }
    const data = new FormData(form);
    const name = String(data.get('name') ?? '').trim();
    const senderEmail = String(data.get('senderEmail') ?? '').trim();
    const subject = String(data.get('subject') ?? '').trim() || 'BizSpace customer message';
    const message = String(data.get('message') ?? '').trim();
    const body = `Name: ${name}\nEmail: ${senderEmail}\n\n${message}`;
    const encodedRecipient = encodeURIComponent(recipient);
    const encodedSubject = encodeURIComponent(subject);
    const encodedBody = encodeURIComponent(body);
    if (provider === 'default') {
      this.contactFeedback.set(
        'Your default email app should open. Review the message there and press Send.',
      );
      window.location.href = `mailto:${recipient}?subject=${encodedSubject}&body=${encodedBody}`;
      return;
    }
    const composeUrl =
      provider === 'gmail'
        ? `https://mail.google.com/mail/?view=cm&fs=1&to=${encodedRecipient}&su=${encodedSubject}&body=${encodedBody}`
        : `https://outlook.office.com/mail/deeplink/compose?to=${encodedRecipient}&subject=${encodedSubject}&body=${encodedBody}`;
    window.open(composeUrl, '_blank', 'noopener,noreferrer');
    this.contactFeedback.set(
      `Your ${provider === 'gmail' ? 'Gmail' : 'Outlook Web'} compose window should open. Review the message there and press Send.`,
    );
  }
  protected displayStatus(value: string): string {
    return value
      .toLowerCase()
      .replaceAll('_', ' ')
      .replace(/\b\w/g, (character) => character.toUpperCase());
  }
  protected customerName(userId: number): string {
    const knownUser = this.state.users().find((user) => user.id === userId);
    if (knownUser?.displayName) return knownUser.displayName;
    const orderCustomerName = this.state.orders().find((order) => order.userId === userId)?.customerName;
    if (orderCustomerName) return orderCustomerName;
    const currentUser = this.state.currentUser();
    if (currentUser?.id === userId) return currentUser.displayName;
    return `Customer #${userId}`;
  }
  protected customerEmail(userId: number): string {
    const knownUser = this.state.users().find((user) => user.id === userId);
    if (knownUser?.email) return knownUser.email;
    const orderCustomerEmail = this.state.orders().find((order) => order.userId === userId)?.customerEmail;
    if (orderCustomerEmail) return orderCustomerEmail;
    const currentUser = this.state.currentUser();
    return currentUser?.id === userId ? currentUser.email : 'Email unavailable';
  }
  protected orderAddress(order: OrderDto): string {
    if (order.deliveryMethod.toUpperCase() === 'PICKUP') return 'Store pickup';
    const address = order.shippingAddress;
    return [address.streetAddress, address.city, address.postalCode, address.country]
      .filter((part) => Boolean(part?.trim()))
      .join(', ') || 'Address unavailable';
  }
  protected orderItemImage(item: OrderLineDto): string | null {
    const responseImage = item.imageUrl?.trim();
    if (responseImage) return responseImage;
    if (item.productId === null) return null;
    return this.state.productViews().find((product) => product.id === item.productId)?.imageUrl ?? null;
  }
  protected isOrderExpanded(orderId: number): boolean {
    return this.expandedOrderIds().has(orderId);
  }
  protected toggleOrderDetails(orderId: number): void {
    this.expandedOrderIds.update((expanded) => {
      const next = new Set(expanded);
      if (next.has(orderId)) next.delete(orderId);
      else next.add(orderId);
      return next;
    });
  }
  protected isSaved(productId: number): boolean {
    return this.state.wishlist().some((item) => item.productId === productId);
  }
  protected productCanBeAdded(product: ProductView): boolean {
    return product.stock > 0;
  }
  protected nextOrderStatus(status: string): 'Paid' | 'Shipped' | 'Delivered' | null {
    switch (status.toUpperCase()) {
      case 'PENDING':
        return 'Paid';
      case 'PAID':
        return 'Shipped';
      case 'SHIPPED':
        return 'Delivered';
      default:
        return null;
    }
  }
  protected canCancelOrder(status: string): boolean {
    return ['PENDING', 'PAID', 'SHIPPED'].includes(status.toUpperCase());
  }
  protected nextOrderAction(status: string): string {
    switch (status.toUpperCase()) {
      case 'PENDING':
        return 'Mark paid';
      case 'PAID':
        return 'Pack order';
      case 'SHIPPED':
        return 'Mark delivered';
      default:
        return '';
    }
  }
  protected statusClass(status: string): string {
    return `status-pill status-${status.toLowerCase().replaceAll('_', '-')}`;
  }
  protected cartImage(lineImage: string | null, productId: number): string | null {
    return (
      lineImage ??
      this.state.productViews().find((product) => product.id === productId)?.imageUrl ??
      null
    );
  }
  protected categoryProductCount(categoryId: number): number {
    return this.state.categoryProductCount(categoryId);
  }
  protected categoryIcon(name: string): string {
    return name.trim().charAt(0).toUpperCase() || '•';
  }
  protected categoryPath(categoryId: number): string {
    const category = this.state.allCategories().find((item) => item.id === categoryId);
    const parent = category?.parentId
      ? this.state.allCategories().find((item) => item.id === category.parentId)
      : null;
    return parent ? `${parent.name} · ${category?.name ?? ''}` : (category?.name ?? '');
  }
  protected categoryIconType(name: string): string {
    const value = name.toLowerCase();
    if (/fashion|clothing|apparel/.test(value)) return 'fashion';
    if (/electronic|accessor|tech/.test(value)) return 'electronics';
    if (/book|stationer/.test(value)) return 'books';
    if (/home|kitchen|furniture/.test(value)) return 'home';
    if (/beauty|personal care|skin|makeup/.test(value)) return 'beauty';
    if (/sport|outdoor|fitness/.test(value)) return 'sports';
    if (/toy|baby|kids|child/.test(value)) return 'toys';
    if (/grocery|food|gourmet/.test(value)) return 'grocery';
    if (/pet|animal/.test(value)) return 'pets';
    if (/auto|car|vehicle/.test(value)) return 'automotive';
    return 'general';
  }
  protected openAuth(mode: AuthMode = 'login'): void {
    this.authError.set('');
    this.authMode.set(mode);
    this.authOpen.set(true);
  }
  protected closeAuth(): void {
    this.authOpen.set(false);
    this.authError.set('');
  }

  protected async login(form: HTMLFormElement): Promise<void> {
    const data = new FormData(form);
    this.authBusy.set(true);
    this.authError.set('');
    try {
      const response = await firstValueFrom(
        this.api.post<AuthResponse>('/auth/login', {
          email: data.get('email'),
          password: data.get('password'),
        }),
      );
      this.auth.accept(response);
      this.state.role.set(response.role);
      this.closeAuth();
      await this.state.loadWorkspace();
      void this.router.navigateByUrl(this.pathForSection(this.state.section()), {
        replaceUrl: true,
      });
      this.state.notify(
        `Welcome${this.state.currentUser()?.displayName ? `, ${this.state.currentUser()?.displayName}` : ''}`,
      );
    } catch (error) {
      this.authError.set(error instanceof Error ? error.message : 'Sign in failed.');
    } finally {
      this.authBusy.set(false);
    }
  }

  protected async register(form: HTMLFormElement): Promise<void> {
    const data = new FormData(form);
    const email = String(data.get('email') ?? '').trim();
    this.authBusy.set(true);
    this.authError.set('');
    try {
      const body = { displayName: data.get('displayName'), email, password: data.get('password') };
      if (this.managerRegistration())
        await firstValueFrom(this.api.post<unknown>('/users/store-managers', body));
      else await firstValueFrom(this.api.post<unknown>('/auth/register', body));
      this.pendingEmail.set(email);
      this.authMode.set('verify');
    } catch (error) {
      this.authError.set(error instanceof Error ? error.message : 'Account creation failed.');
    } finally {
      this.authBusy.set(false);
    }
  }

  protected async verifyEmail(form: HTMLFormElement): Promise<void> {
    const code = String(new FormData(form).get('code') ?? '').trim();
    this.authBusy.set(true);
    this.authError.set('');
    try {
      const response = await firstValueFrom(
        this.api.post<AuthResponse>('/auth/verify-email', { email: this.pendingEmail(), code }),
      );
      this.auth.accept(response);
      this.state.role.set(response.role);
      this.closeAuth();
      await this.state.loadWorkspace();
      void this.router.navigateByUrl(this.pathForSection(this.state.section()), {
        replaceUrl: true,
      });
      this.state.notify('Your email is verified. Welcome to BizSpace.');
    } catch (error) {
      this.authError.set(error instanceof Error ? error.message : 'Email verification failed.');
    } finally {
      this.authBusy.set(false);
    }
  }

  protected async resendVerificationCode(): Promise<void> {
    this.authBusy.set(true);
    this.authError.set('');
    try {
      await firstValueFrom(
        this.api.post<unknown>('/auth/resend-verification', { email: this.pendingEmail() }),
      );
      this.state.notify('If the account needs confirmation, a code was sent.');
    } catch (error) {
      this.authError.set(
        error instanceof Error ? error.message : 'Could not resend the verification code.',
      );
    } finally {
      this.authBusy.set(false);
    }
  }

  protected async requestPasswordReset(form: HTMLFormElement): Promise<void> {
    const email = String(new FormData(form).get('email') ?? '').trim();
    this.authBusy.set(true);
    this.authError.set('');
    try {
      await firstValueFrom(this.api.post<unknown>('/auth/forgot-password', { email }));
      this.pendingEmail.set(email);
      this.authMode.set('reset');
    } catch (error) {
      this.authError.set(
        error instanceof Error ? error.message : 'Could not request a reset code.',
      );
    } finally {
      this.authBusy.set(false);
    }
  }

  protected async resetPassword(form: HTMLFormElement): Promise<void> {
    const data = new FormData(form);
    this.authBusy.set(true);
    this.authError.set('');
    try {
      await firstValueFrom(
        this.api.post<unknown>('/auth/reset-password', {
          email: this.pendingEmail(),
          code: data.get('code'),
          newPassword: data.get('newPassword'),
        }),
      );
      this.authMode.set('login');
      this.state.notify('Password reset. Sign in with your new password.');
    } catch (error) {
      this.authError.set(error instanceof Error ? error.message : 'Password reset failed.');
    } finally {
      this.authBusy.set(false);
    }
  }

  protected logout(): void {
    this.auth.clear();
    this.state.clearPrivateState();
    this.state.section.set('Discover');
    void this.router.navigateByUrl('/discover', { replaceUrl: true });
    this.state.notify('You have signed out');
  }
  protected async openCart(): Promise<void> {
    if (!this.auth.isAuthenticated()) {
      this.openAuth('login');
      return;
    }
    try {
      this.state.cart.set(await firstValueFrom(this.api.get('/cart')));
      this.state.setCartOpen(true);
    } catch (error) {
      this.state.handleError(error);
    }
  }
  protected async addToCart(productId: number): Promise<void> {
    if (!this.auth.isAuthenticated()) {
      this.openAuth('login');
      return;
    }
    await this.state.addToCart(productId);
  }
  protected async toggleSaved(productId: number): Promise<void> {
    if (!this.auth.isAuthenticated()) {
      this.openAuth('login');
      return;
    }
    await this.state.toggleSaved(productId);
  }
  protected async updateCartQuantity(lineId: number, quantity: number): Promise<void> {
    await this.state.updateCartQuantity(lineId, quantity);
  }
  protected async saveProfile(form: HTMLFormElement): Promise<void> {
    const userId = this.auth.userId();
    if (!userId) return;
    const displayName = String(new FormData(form).get('displayName') ?? '').trim();
    this.profileBusy.set(true);
    this.profileError.set('');
    try {
      const updated = await firstValueFrom(
        this.api.put<UserDto>(`/users/${userId}`, { displayName }),
      );
      this.state.currentUser.set(updated);
      this.state.notify('Profile updated');
    } catch (error) {
      this.profileError.set(
        error instanceof Error ? error.message : 'Profile could not be updated.',
      );
    } finally {
      this.profileBusy.set(false);
    }
  }
  protected async changePassword(form: HTMLFormElement): Promise<void> {
    const data = new FormData(form);
    const currentPassword = String(data.get('currentPassword') ?? '');
    const newPassword = String(data.get('newPassword') ?? '');
    const confirmPassword = String(data.get('confirmPassword') ?? '');
    this.passwordError.set('');
    if (newPassword !== confirmPassword) {
      this.passwordError.set('The new passwords do not match.');
      return;
    }
    this.passwordBusy.set(true);
    try {
      await firstValueFrom(
        this.api.post<unknown>('/auth/change-password', { currentPassword, newPassword }),
      );
      form.reset();
      this.state.notify('Password changed successfully');
    } catch (error) {
      this.passwordError.set(
        error instanceof Error ? error.message : 'Password could not be changed.',
      );
    } finally {
      this.passwordBusy.set(false);
    }
  }
  protected openAddressEditor(address?: SavedAddressDto): void {
    this.editingAddress.set(address ?? null);
    this.addressEditorOpen.set(true);
  }
  protected closeAddressEditor(): void {
    this.addressEditorOpen.set(false);
    this.editingAddress.set(null);
  }
  protected async saveAddress(form: HTMLFormElement): Promise<void> {
    const data = new FormData(form);
    const address: SavedAddressWriteRequest = {
      label: String(data.get('label') ?? '').trim(),
      streetAddress: String(data.get('streetAddress') ?? '').trim(),
      city: String(data.get('city') ?? '').trim(),
      postalCode: String(data.get('postalCode') ?? '').trim(),
      country: String(data.get('country') ?? '').trim(),
    };
    this.addressBusy.set(true);
    try {
      if (await this.state.saveAddress(address, this.editingAddress()?.id))
        this.closeAddressEditor();
    } finally {
      this.addressBusy.set(false);
    }
  }
  protected async deleteSavedAddress(address: SavedAddressDto): Promise<void> {
    if (
      typeof window !== 'undefined' &&
      window.confirm(`Delete the saved address “${address.label}”?`)
    )
      await this.state.deleteAddress(address.id);
  }
  protected beginCheckout(): void {
    this.selectedAddressId.set(null);
    this.checkoutMode.set(true);
  }
  protected useSavedAddress(value: string, form: HTMLFormElement): void {
    const id = Number(value);
    const address = this.state.addresses().find((item) => item.id === id);
    this.selectedAddressId.set(address?.id ?? null);
    (form.elements.namedItem('streetAddress') as HTMLInputElement).value =
      address?.streetAddress ?? '';
    (form.elements.namedItem('city') as HTMLInputElement).value = address?.city ?? '';
    (form.elements.namedItem('postalCode') as HTMLInputElement).value = address?.postalCode ?? '';
    (form.elements.namedItem('country') as HTMLInputElement).value = address?.country ?? '';
  }
  protected async placeOrder(form: HTMLFormElement): Promise<void> {
    const address = new FormData(form);
    await this.state.checkout(
      this.paymentMethod(),
      this.deliveryMethod(),
      {
        streetAddress: String(address.get('streetAddress') ?? ''),
        city: String(address.get('city') ?? ''),
        postalCode: String(address.get('postalCode') ?? ''),
        country: String(address.get('country') ?? ''),
      },
      this.selectedAddressId(),
    );
    if (!this.state.cartCount() && !this.state.cartOpen()) this.checkoutMode.set(false);
  }

  protected openProductModal(product?: ProductView): void {
    this.editingProduct.set(product ?? null);
    this.productModalOpen.set(true);
  }
  protected closeProductModal(): void {
    this.productModalOpen.set(false);
    this.editingProduct.set(null);
  }
  protected async uploadProductPhoto(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    const product = this.editingProduct();
    if (!file || !product) return;
    if (product.images.length >= 10) {
      this.state.notify('A product can have at most 10 photos');
      input.value = '';
      return;
    }
    this.productPhotoBusy.set(true);
    try {
      if (await this.state.uploadProductImage(product.id, file))
        this.editingProduct.set(
          this.state.productViews().find((item) => item.id === product.id) ?? product,
        );
    } finally {
      this.productPhotoBusy.set(false);
      input.value = '';
    }
  }
  protected async moveProductPhoto(
    product: ProductView,
    index: number,
    offset: number,
  ): Promise<void> {
    const images = product.images.slice().sort((a, b) => a.sortOrder - b.sortOrder);
    const target = index + offset;
    if (target < 0 || target >= images.length) return;
    [images[index], images[target]] = [images[target], images[index]];
    this.productPhotoBusy.set(true);
    try {
      if (
        await this.state.reorderProductImages(
          product.id,
          images.map((image) => image.id),
        )
      )
        this.editingProduct.set(
          this.state.productViews().find((item) => item.id === product.id) ?? product,
        );
    } finally {
      this.productPhotoBusy.set(false);
    }
  }
  protected async deleteProductPhoto(product: ProductView, imageId: number): Promise<void> {
    if (typeof window !== 'undefined' && !window.confirm('Remove this product photo?')) return;
    this.productPhotoBusy.set(true);
    try {
      await this.state.deleteProductImage(product.id, imageId);
      this.editingProduct.set(
        this.state.productViews().find((item) => item.id === product.id) ?? product,
      );
    } finally {
      this.productPhotoBusy.set(false);
    }
  }
  protected async saveProduct(form: HTMLFormElement): Promise<void> {
    const data = new FormData(form);
    const request = {
      name: String(data.get('name') ?? '').trim(),
      brand: String(data.get('brand') ?? '').trim(),
      description: String(data.get('description') ?? '').trim(),
      price: Number(data.get('price')),
      discountPercent: Number(data.get('discountPercent') ?? 0),
      storeId: Number(data.get('storeId')),
      categoryId: Number(data.get('categoryId')),
      stock: Number(data.get('stock') ?? 0),
      minimumStockLevel: Number(data.get('minimumStockLevel') ?? 0),
    };
    const image = (data.get('image') as File | null)?.size ? (data.get('image') as File) : null;
    const current = this.editingProduct();
    if (
      !request.name ||
      !request.description ||
      request.price <= 0 ||
      !request.storeId ||
      !request.categoryId
    )
      return;
    this.state.apiError.set('');
    if (current) await this.state.updateProduct(current.id, request, image);
    else await this.state.createProduct(request, image);
    if (!this.state.apiError()) {
      this.productModalOpen.set(false);
      this.editingProduct.set(null);
    }
  }

  protected openStoreModal(): void {
    this.storeModalOpen.set(true);
  }
  protected async saveStore(form: HTMLFormElement): Promise<void> {
    const data = new FormData(form);
    const existingCount = this.state.myStores().length;
    this.state.apiError.set('');
    await this.state.createStore(
      String(data.get('name') ?? '').trim(),
      String(data.get('description') ?? '').trim(),
    );
    if (this.state.myStores().length > existingCount) this.storeModalOpen.set(false);
  }
  protected openCampaignModal(campaign?: MarketingCampaignDto): void {
    this.editingCampaign.set(campaign ?? null);
    this.campaignTarget.set(
      campaign?.productIds.length ? 'products' : campaign?.categoryId ? 'category' : 'store',
    );
    this.campaignStoreId.set(
      campaign?.storeId ?? this.state.selectedStoreId() ?? this.state.myStores()[0]?.id ?? null,
    );
    this.campaignProductIds.set(new Set(campaign?.productIds ?? []));
    this.campaignModalOpen.set(true);
  }
  protected setCampaignTarget(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    if (value === 'category' || value === 'products' || value === 'store')
      this.campaignTarget.set(value);
  }
  protected setCampaignStore(event: Event): void {
    this.campaignStoreId.set(Number((event.target as HTMLSelectElement).value) || null);
  }
  protected toggleCampaignProduct(productId: number, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.campaignProductIds.update((current) => {
      const next = new Set(current);
      if (checked) next.add(productId);
      else next.delete(productId);
      return next;
    });
  }
  protected async saveCampaign(form: HTMLFormElement): Promise<void> {
    const data = new FormData(form);
    const target = String(data.get('targetType') ?? 'store') as 'store' | 'category' | 'products';
    const selectedStoreId = target === 'products' ? null : this.campaignStoreId();
    const categoryId = target === 'category' ? Number(data.get('categoryId')) || null : null;
    const productIds = target === 'products' ? [...this.campaignProductIds()] : [];
    this.state.apiError.set('');
    if (target !== 'products' && !selectedStoreId) {
      this.state.apiError.set('Create or choose one of your stores for this campaign.');
      return;
    }
    if (target === 'category' && !categoryId) {
      this.state.apiError.set('Choose a category that has products in the selected store.');
      return;
    }
    if (target === 'products' && productIds.length === 0) {
      this.state.apiError.set('Select at least one product for this campaign.');
      return;
    }
    const request = {
      name: String(data.get('name') ?? '').trim(),
      description: String(data.get('description') ?? '').trim(),
      discountPercent: Number(data.get('discountPercent')),
      startsAtUtc: new Date(String(data.get('startsAt'))).toISOString(),
      endsAtUtc: new Date(String(data.get('endsAt'))).toISOString(),
      isEnabled: data.get('isEnabled') === 'on',
      storeId: selectedStoreId,
      categoryId,
      productIds,
    };
    const existing = this.editingCampaign();
    if (existing) await this.state.updateCampaign(existing, request);
    else await this.state.createCampaign(request);
    if (!this.state.apiError()) {
      this.campaignModalOpen.set(false);
      this.editingCampaign.set(null);
    }
  }

  protected openCategoryModal(category?: CategoryDto): void {
    this.editingCategory.set(category ?? null);
    this.categoryModalOpen.set(true);
  }
  protected async saveCategory(form: HTMLFormElement): Promise<void> {
    const data = new FormData(form);
    const imageUrl = String(data.get('imageUrl') ?? '').trim();
    const request = {
      name: String(data.get('name') ?? '').trim(),
      description: String(data.get('description') ?? '').trim(),
      parentId: Number(data.get('parentId')) || null,
      imageUrl: imageUrl || null,
    };
    const existing = this.editingCategory();
    this.state.apiError.set('');
    if (existing) await this.state.updateCategory(existing, request);
    else await this.state.createCategory(request);
    if (!this.state.apiError()) {
      this.categoryModalOpen.set(false);
      this.editingCategory.set(null);
    }
  }

  protected async removeCategory(category: CategoryDto): Promise<void> {
    if (typeof window !== 'undefined' && window.confirm(`Delete “${category.name}”?`))
      await this.state.deleteCategory(category.id);
  }
  protected async removeProduct(product: ProductView): Promise<void> {
    const ownerContext = this.auth.role() === 'Admin' ? 'the marketplace' : 'your store';
    if (
      typeof window !== 'undefined' &&
      window.confirm(
        `Delete “${product.name}” from ${ownerContext}? It will be removed from active carts and wishlists. Existing order history will be kept.`,
      )
    )
      await this.state.deleteProduct(product.id);
  }
  protected async removeStore(storeId: number, name: string): Promise<void> {
    if (
      typeof window !== 'undefined' &&
      window.confirm(`Delete ${name} and its catalog? This action cannot be undone.`)
    )
      await this.state.deleteStore(storeId);
  }
  protected async removeCampaign(campaign: MarketingCampaignDto): Promise<void> {
    if (typeof window !== 'undefined' && window.confirm(`Delete campaign “${campaign.name}”?`))
      await this.state.deleteCampaign(campaign.id);
  }
  protected async sendMessage(storeId: number): Promise<void> {
    const message =
      typeof window !== 'undefined' ? window.prompt('Message to the store manager')?.trim() : '';
    if (message) await this.state.sendManagerMessage(storeId, message);
  }
  protected exportAnalytics(): void {
    const report = this.state.analytics();
    if (!report || typeof document === 'undefined') return;
    const escapeCell = (value: string | number) => `"${String(value).replaceAll('"', '""')}"`;
    const rows: (string | number)[][] = [
      ['Metric', 'Value'],
      ['Revenue', report.revenue],
      ['Total orders', report.totalOrders],
      ['Paid orders', report.paidOrders],
      ['Cancelled orders', report.cancelledOrders],
      ['Average order value', report.averageOrderValue],
      ['Units sold', report.unitsSold],
      [],
      ['Top product', 'Quantity sold', 'Revenue'],
      ...report.topProducts.map((product) => [
        product.productName,
        product.quantitySold,
        product.revenue,
      ]),
    ];
    const csv = rows.map((row) => row.map(escapeCell).join(',')).join('\r\n');
    const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv;charset=utf-8' }));
    const link = document.createElement('a');
    link.href = url;
    link.download = `bizspace-sales-${new Date().toISOString().slice(0, 10)}.csv`;
    link.click();
    URL.revokeObjectURL(url);
  }
  protected async cancelOrder(id: number): Promise<void> {
    if (typeof window !== 'undefined' && window.confirm(`Cancel order #${id}?`))
      await this.state.cancelOrder(id);
  }
  protected async advanceOrder(
    id: number,
    status: 'Paid' | 'Shipped' | 'Delivered',
  ): Promise<void> {
    await this.state.advanceOrder(id, status);
  }
  protected trackById(_: number, item: { id: number }): number {
    return item.id;
  }
}
