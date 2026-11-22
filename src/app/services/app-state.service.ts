import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import { AuthSessionService, WorkspaceRole } from './auth-session.service';

export type CustomerSection =
  | 'Discover'
  | 'Shop'
  | 'Stores'
  | 'My orders'
  | 'Wishlist'
  | 'Profile'
  | 'Address book'
  | 'About us'
  | 'Contact us';
export type ManagerSection = 'Overview' | 'Products' | 'Orders' | 'Campaigns' | 'Reviews';
export type AdminSection =
  'Overview' | 'Products' | 'Stores' | 'Customers' | 'Categories' | 'Orders';
export type WorkspaceSection = CustomerSection | ManagerSection | AdminSection;

export interface UserDto {
  id: number;
  displayName: string;
  email: string;
  role: WorkspaceRole;
  createdAtUtc: string;
}
export interface StoreDto {
  id: number;
  managerId: number | null;
  name: string;
  description: string;
  createdAtUtc: string;
}
export interface ProductImageDto {
  id: number;
  secureUrl: string;
  sortOrder: number;
}
export interface ProductDto {
  id: number;
  storeId: number;
  name: string;
  brand: string;
  description: string;
  price: number;
  discountPercent: number;
  effectivePrice: number;
  categoryId: number;
  stock: number;
  minimumStockLevel: number;
  images: ProductImageDto[];
  createdAtUtc: string;
  averageRating: number;
  reviewCount: number;
}
export interface ProductView extends ProductDto {
  categoryName: string;
  storeName: string;
  imageUrl: string | null;
}
export interface ProductReviewDto {
  id: number;
  userId: number;
  displayName: string;
  productId: number;
  rating: number;
  comment: string;
  createdAtUtc: string;
}
export interface CategoryDto {
  id: number;
  name: string;
  parentId: number | null;
  description: string;
  imageUrl: string | null;
  children: CategoryDto[];
  products?: ProductDto[];
}
export interface ProductPage {
  items: ProductDto[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
export interface CartLineDto {
  id: number;
  productId: number;
  productName: string;
  imageUrl: string | null;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
  availableStock: number;
}
export interface CartDto {
  id: number;
  userId: number;
  items: CartLineDto[];
  subtotal: number;
  itemCount: number;
}
export interface ShippingAddress {
  streetAddress: string;
  city: string;
  postalCode: string;
  country: string;
}
export interface OrderLineDto {
  productId: number | null;
  productName: string;
  imageUrl?: string | null;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}
export interface OrderDto {
  id: number;
  userId: number;
  customerName?: string;
  customerEmail?: string;
  status: string;
  totalAmount: number;
  shippingAddress: ShippingAddress;
  paymentMethod: string;
  deliveryMethod: string;
  createdAtUtc: string;
  items: OrderLineDto[];
}
export interface WishlistDto {
  productId: number;
  productName: string;
  price: number;
  imageUrl: string | null;
  addedAtUtc: string;
}
export interface MarketingCampaignDto {
  id: number;
  name: string;
  description: string;
  discountPercent: number;
  startsAtUtc: string;
  endsAtUtc: string;
  isEnabled: boolean;
  isCurrentlyActive: boolean;
  storeId: number | null;
  categoryId: number | null;
  productIds: number[];
}
export interface SalesAnalyticsDto {
  fromUtc: string;
  toUtc: string;
  totalOrders: number;
  paidOrders: number;
  cancelledOrders: number;
  revenue: number;
  averageOrderValue: number;
  unitsSold: number;
  topProducts: { productName: string; quantitySold: number; revenue: number }[];
}
export interface NotificationDto {
  id: number;
  type: string;
  title: string;
  message: string;
  createdAtUtc: string;
  readAtUtc: string | null;
  isRead: boolean;
}
export interface SavedAddressDto {
  id: number;
  label: string;
  streetAddress: string;
  city: string;
  postalCode: string;
  country: string;
  createdAtUtc?: string;
}
export interface SavedAddressWriteRequest {
  label: string;
  streetAddress: string;
  city: string;
  postalCode: string;
  country: string;
}

@Injectable({ providedIn: 'root' })
export class AppStateService {
  readonly role = signal<WorkspaceRole | null>(null);
  readonly section = signal<WorkspaceSection>('Discover');
  readonly currentUser = signal<UserDto | null>(null);
  readonly search = signal('');
  readonly categorySearch = signal('');
  readonly storeSearch = signal('');
  readonly customerSearch = signal('');
  readonly orderSearch = signal('');
  readonly categoryId = signal<number | null>(null);
  readonly subcategoryId = signal<number | null>(null);
  readonly brandFilter = signal('');
  readonly minPrice = signal<number | null>(null);
  readonly maxPrice = signal<number | null>(null);
  readonly cartOpen = signal(false);
  readonly toast = signal('');
  readonly loading = signal(false);
  readonly catalogLoading = signal(true);
  readonly apiError = signal('');
  readonly categories = signal<CategoryDto[]>([]);
  readonly products = signal<ProductDto[]>([]);
  readonly productTotalCount = signal(0);
  readonly productPage = signal(1);
  readonly stores = signal<StoreDto[]>([]);
  readonly customerStoreId = signal<number | null>(null);
  readonly selectedProductId = signal<number | null>(null);
  readonly myStores = signal<StoreDto[]>([]);
  readonly users = signal<UserDto[]>([]);
  readonly orders = signal<OrderDto[]>([]);
  readonly cart = signal<CartDto | null>(null);
  readonly wishlist = signal<WishlistDto[]>([]);
  readonly campaigns = signal<MarketingCampaignDto[]>([]);
  readonly analytics = signal<SalesAnalyticsDto | null>(null);
  readonly notifications = signal<NotificationDto[]>([]);
  readonly addresses = signal<SavedAddressDto[]>([]);
  readonly selectedStoreId = signal<number | null>(null);
  readonly auth = inject(AuthSessionService);
  private readonly api = inject(ApiService);
  private toastTimer?: ReturnType<typeof setTimeout>;
  private initialization?: Promise<void>;

  readonly allCategories = computed(() => {
    const flattened: CategoryDto[] = [];
    const visit = (categories: CategoryDto[]) =>
      categories.forEach((category) => {
        flattened.push(category);
        visit(category.children ?? []);
      });
    visit(this.categories());
    return flattened;
  });
  readonly categoryOptions = computed(() => [
    { id: 0, name: 'All' },
    ...this.allCategories().map(({ id, name }) => ({ id, name })),
  ]);
  readonly topLevelCategories = computed(() => this.categories());
  readonly filterSubcategories = computed(() => {
    const allCategories = this.allCategories();
    const parentId = this.categoryId();
    if (!parentId) return allCategories.filter((category) => category.parentId !== null);
    const parent = allCategories.find((category) => category.id === parentId);
    return parent?.children ?? [];
  });
  readonly brandOptions = computed(() =>
    [
      ...new Set(
        this.productViews()
          .map((product) => product.brand.trim())
          .filter(Boolean),
      ),
    ].sort((a, b) => a.localeCompare(b)),
  );
  readonly selectedCategoryName = computed(
    () =>
      this.allCategories().find(
        (category) => category.id === (this.subcategoryId() ?? this.categoryId()),
      )?.name ?? 'All the good things',
  );
  private readonly activeCategoryIds = computed<Set<number> | null>(() => {
    const targetId = this.subcategoryId() ?? this.categoryId();
    if (!targetId) return null;
    const categories = this.allCategories();
    const root = categories.find((category) => category.id === targetId);
    if (!root) return new Set([targetId]);
    const ids = new Set<number>();
    const visit = (category: CategoryDto) => {
      ids.add(category.id);
      category.children?.forEach(visit);
    };
    visit(root);
    return ids;
  });
  readonly productViews = computed<ProductView[]>(() => {
    const categories = new Map(
      this.allCategories().map((category) => [category.id, category.name]),
    );
    const stores = new Map(
      [...this.stores(), ...this.myStores()].map((store) => [store.id, store.name]),
    );
    return this.products().map((product) => ({
      ...product,
      categoryName: categories.get(product.categoryId) ?? 'Uncategorized',
      storeName: stores.get(product.storeId) ?? '',
      imageUrl:
        product.images.find((image) => image.sortOrder === 0)?.secureUrl ??
        product.images[0]?.secureUrl ??
        null,
    }));
  });
  readonly productResults = computed(() => {
    const term = this.search().trim().toLowerCase();
    const activeCategoryIds = this.activeCategoryIds();
    const brand = this.brandFilter().trim().toLowerCase();
    const minPrice = this.minPrice();
    const maxPrice = this.maxPrice();
    const isCustomerStorefront =
      this.section() === 'Stores' && this.role() !== 'Admin' && this.role() !== 'StoreManager';
    const activeStoreId =
      this.role() === 'StoreManager'
        ? this.selectedStoreId()
        : isCustomerStorefront
          ? this.customerStoreId()
          : null;
    return this.productViews().filter(
      (product) =>
        (!activeCategoryIds || activeCategoryIds.has(product.categoryId)) &&
        (!activeStoreId || product.storeId === activeStoreId) &&
        (!brand || product.brand.trim().toLowerCase() === brand) &&
        (minPrice === null || product.effectivePrice >= minPrice) &&
        (maxPrice === null || product.effectivePrice <= maxPrice) &&
        (!term ||
          `${product.name} ${product.brand} ${product.description} ${product.categoryName}`
            .toLowerCase()
            .includes(term)),
    );
  });
  readonly productCount = computed(() => this.products().length);
  readonly cartCount = computed(() => this.cart()?.itemCount ?? 0);
  readonly cartSubtotal = computed(() => this.cart()?.subtotal ?? 0);
  readonly savedProducts = computed(() => {
    const savedIds = new Set(this.wishlist().map((item) => item.productId));
    return this.productViews().filter((product) => savedIds.has(product.id));
  });
  readonly lowStockProducts = computed(() =>
    this.productViews().filter((product) => product.stock <= product.minimumStockLevel),
  );
  readonly openOrders = computed(() =>
    this.orders().filter((order) =>
      ['PENDING', 'PAID', 'SHIPPED'].includes(order.status.toUpperCase()),
    ),
  );
  readonly unreadNotifications = computed(
    () => this.notifications().filter((item) => !item.isRead).length,
  );
  readonly selectedStore = computed(
    () => this.myStores().find((store) => store.id === this.selectedStoreId()) ?? null,
  );
  readonly selectedCustomerStore = computed(
    () => this.stores().find((store) => store.id === this.customerStoreId()) ?? null,
  );
  readonly filteredCategories = computed(() => {
    const query = this.categorySearch().trim().toLowerCase();
    return this.allCategories().filter((category) => category.name.toLowerCase().includes(query));
  });
  readonly filteredStores = computed(() => {
    const query = this.storeSearch().trim().toLowerCase();
    return this.stores().filter((store) => `${store.name} ${store.description}`.toLowerCase().includes(query));
  });
  readonly filteredUsers = computed(() => {
    const query = this.customerSearch().trim().toLowerCase();
    return this.users().filter((user) => `${user.displayName} ${user.email} ${user.role}`.toLowerCase().includes(query));
  });
  readonly filteredOrders = computed(() => {
    const query = this.orderSearch().trim().toLowerCase();
    return this.orders().filter((order) => `${order.id} ${order.userId} ${order.status}`.toLowerCase().includes(query));
  });
  readonly selectedProduct = computed(
    () => this.productViews().find((product) => product.id === this.selectedProductId()) ?? null,
  );

  initialize(): Promise<void> {
    this.initialization ??= this.loadInitialState();
    return this.initialization;
  }

  private async loadInitialState(): Promise<void> {
    this.catalogLoading.set(true);
    await Promise.all([this.loadCatalog(), this.loadPublicStores()]);
    this.catalogLoading.set(false);
    if (this.auth.isAuthenticated()) await this.loadWorkspace();
  }

  async loadCatalog(page = 1, append = false): Promise<void> {
    this.apiError.set('');
    try {
      const [categories, result] = await Promise.all([
        firstValueFrom(this.api.get<CategoryDto[]>('/categories')),
        firstValueFrom(this.api.get<ProductPage>('/products', { page, pageSize: 100 })),
      ]);
      this.categories.set(categories ?? []);
      this.productTotalCount.set(result?.totalCount ?? 0);
      this.productPage.set(page);
      this.products.update((current) =>
        append ? [...current, ...(result?.items ?? [])] : (result?.items ?? []),
      );
    } catch (error) {
      this.apiError.set(this.errorMessage(error));
    }
  }

  private async refreshCategories(): Promise<void> {
    this.categories.set((await firstValueFrom(this.api.get<CategoryDto[]>('/categories'))) ?? []);
  }

  private async loadPublicStores(): Promise<void> {
    try {
      this.stores.set((await firstValueFrom(this.api.get<StoreDto[]>('/stores'))) ?? []);
    } catch {
      this.stores.set([]);
    }
  }

  async loadMoreProducts(): Promise<void> {
    if (this.products().length >= this.productTotalCount()) return;
    await this.loadCatalog(this.productPage() + 1, true);
  }

  async loadWorkspace(): Promise<void> {
    const role = this.auth.role();
    const userId = this.auth.userId();
    if (!role || !userId) return;
    this.role.set(role);
    this.loading.set(true);
    this.apiError.set('');
    try {
      const baseCalls = [
        firstValueFrom(this.api.get<UserDto>(`/users/${userId}`)),
        firstValueFrom(this.api.get<NotificationDto[]>('/notifications')),
      ] as const;
      const [user, notifications] = await Promise.all(baseCalls);
      this.currentUser.set(user);
      this.notifications.set(notifications ?? []);

      if (role === 'Customer') {
        const [cart, wishlist, orders, addresses] = await Promise.all([
          firstValueFrom(this.api.get<CartDto>('/cart')),
          firstValueFrom(this.api.get<WishlistDto[]>('/wishlist')),
          firstValueFrom(this.api.get<OrderDto[]>('/orders')),
          firstValueFrom(this.api.get<SavedAddressDto[]>('/addresses')),
        ]);
        this.cart.set(cart);
        this.wishlist.set(wishlist ?? []);
        this.orders.set(orders ?? []);
        this.addresses.set(addresses ?? []);
        this.section.set('Discover');
      } else if (role === 'StoreManager') {
        const [myStores, orders, campaigns, analytics] = await Promise.all([
          firstValueFrom(this.api.get<StoreDto[]>('/stores/mine')),
          firstValueFrom(this.api.get<OrderDto[]>('/orders')),
          firstValueFrom(this.api.get<MarketingCampaignDto[]>('/store-manager/campaigns')),
          firstValueFrom(this.api.get<SalesAnalyticsDto>('/store-manager/analytics/sales')),
        ]);
        this.myStores.set(myStores ?? []);
        this.selectedStoreId.set(
          this.selectedStoreId() && myStores?.some((store) => store.id === this.selectedStoreId())
            ? this.selectedStoreId()
            : (myStores?.[0]?.id ?? null),
        );
        this.orders.set(orders ?? []);
        this.campaigns.set(campaigns ?? []);
        this.analytics.set(analytics ?? null);
        this.section.set('Overview');
        await this.loadManagerProducts();
      } else {
        const [users, stores, orders] = await Promise.all([
          firstValueFrom(this.api.get<UserDto[]>('/users')),
          firstValueFrom(this.api.get<StoreDto[]>('/stores')),
          firstValueFrom(this.api.get<OrderDto[]>('/orders')),
        ]);
        this.users.set(users ?? []);
        this.stores.set(stores ?? []);
        this.orders.set(orders ?? []);
        this.section.set('Overview');
        await this.loadAdminProducts();
      }
    } catch (error) {
      this.apiError.set(this.errorMessage(error));
      if (this.auth.isAuthenticated()) this.notify(this.errorMessage(error));
      else {
        this.clearPrivateState();
      }
    } finally {
      this.loading.set(false);
    }
  }

  private async loadManagerProducts(): Promise<void> {
    const ids = this.myStores().map((store) => store.id);
    if (!ids.length) {
      this.products.set([]);
      this.productTotalCount.set(0);
      return;
    }
    try {
      const pages = await Promise.all(
        ids.map((storeId) =>
          firstValueFrom(
            this.api.get<ProductPage>('/products', { storeId, page: 1, pageSize: 100 }),
          ),
        ),
      );
      const all = pages.flatMap((page) => page.items ?? []);
      this.products.set(all);
      this.productTotalCount.set(all.length);
      this.productPage.set(1);
    } catch (error) {
      this.apiError.set(this.errorMessage(error));
    }
  }

  private async loadAdminProducts(): Promise<void> {
    try {
      const firstPage = await firstValueFrom(
        this.api.get<ProductPage>('/products', { page: 1, pageSize: 100 }),
      );
      const remainingPages = await Promise.all(
        Array.from({ length: Math.max(0, firstPage.totalPages - 1) }, (_, index) =>
          firstValueFrom(
            this.api.get<ProductPage>('/products', { page: index + 2, pageSize: 100 }),
          ),
        ),
      );
      const products = [
        ...(firstPage.items ?? []),
        ...remainingPages.flatMap((page) => page.items ?? []),
      ];
      this.products.set(products);
      this.productTotalCount.set(firstPage.totalCount ?? products.length);
      this.productPage.set(firstPage.totalPages || 1);
    } catch (error) {
      this.apiError.set(this.errorMessage(error));
    }
  }

  async refreshOrders(): Promise<void> {
    this.orders.set((await firstValueFrom(this.api.get<OrderDto[]>('/orders'))) ?? []);
  }

  private async refreshManagerAnalytics(): Promise<void> {
    if (this.auth.role() !== 'StoreManager') return;
    try {
      this.analytics.set(
        await firstValueFrom(this.api.get<SalesAnalyticsDto>('/store-manager/analytics/sales')),
      );
    } catch (error) {
      this.apiError.set(this.errorMessage(error));
    }
  }

  async addToCart(productId: number): Promise<void> {
    if (!this.auth.isAuthenticated()) {
      this.notify('Sign in to add items to your bag.');
      return;
    }
    this.apiError.set('');
    try {
      this.cart.set(
        await firstValueFrom(this.api.post<CartDto>('/cart/items', { productId, quantity: 1 })),
      );
      this.notify('Added to your bag');
    } catch (error) {
      this.handleError(error);
    }
  }

  async updateCartQuantity(lineId: number, quantity: number): Promise<void> {
    this.apiError.set('');
    try {
      this.cart.set(
        quantity <= 0
          ? await firstValueFrom(this.api.delete<CartDto>(`/cart/items/${lineId}`))
          : await firstValueFrom(
              this.api.put<CartDto>(`/cart/items/${lineId}/quantity`, { quantity }),
            ),
      );
    } catch (error) {
      this.handleError(error);
    }
  }

  async checkout(
    paymentMethod: 'Card' | 'Cash',
    deliveryMethod: 'Pickup' | 'Courier',
    shippingAddress: ShippingAddress,
    savedAddressId?: number | null,
  ): Promise<void> {
    this.apiError.set('');
    try {
      const orderRequest =
        deliveryMethod === 'Pickup'
          ? {}
          : savedAddressId
            ? { savedAddressId }
            : { shippingAddress };
      const order = await firstValueFrom(
        this.api.post<OrderDto>(
          `/orders?paymentMethod=${paymentMethod}&deliveryMethod=${deliveryMethod}`,
          orderRequest,
        ),
      );
      this.orders.update((orders) => [order, ...orders]);
      this.cart.set(null);
      this.cartOpen.set(false);
      this.section.set('My orders');
      this.notify(`Order #${order.id} placed`);
    } catch (error) {
      this.handleError(error);
    }
  }

  async toggleSaved(productId: number): Promise<void> {
    this.apiError.set('');
    try {
      const exists = this.wishlist().some((item) => item.productId === productId);
      if (exists) await firstValueFrom(this.api.delete<unknown>(`/wishlist/${productId}`));
      else await firstValueFrom(this.api.post<unknown>(`/wishlist/${productId}`));
      this.wishlist.set((await firstValueFrom(this.api.get<WishlistDto[]>('/wishlist'))) ?? []);
      this.notify(exists ? 'Removed from wishlist' : 'Added to wishlist');
    } catch (error) {
      this.handleError(error);
    }
  }

  async saveAddress(request: SavedAddressWriteRequest, addressId?: number): Promise<boolean> {
    this.apiError.set('');
    try {
      const saved = addressId
        ? await firstValueFrom(this.api.put<SavedAddressDto>(`/addresses/${addressId}`, request))
        : await firstValueFrom(this.api.post<SavedAddressDto>('/addresses', request));
      if (addressId)
        this.addresses.update((items) =>
          items.map((item) => (item.id === addressId ? saved : item)),
        );
      else this.addresses.update((items) => [...items, saved]);
      this.notify(addressId ? 'Address updated' : 'Address saved');
      return true;
    } catch (error) {
      this.handleError(error);
      return false;
    }
  }

  async deleteAddress(addressId: number): Promise<void> {
    this.apiError.set('');
    try {
      await firstValueFrom(this.api.delete<unknown>(`/addresses/${addressId}`));
      this.addresses.update((items) => items.filter((item) => item.id !== addressId));
      this.notify('Address deleted');
    } catch (error) {
      this.handleError(error);
    }
  }

  async cancelOrder(id: number): Promise<void> {
    this.apiError.set('');
    try {
      await firstValueFrom(this.api.post<OrderDto>(`/orders/${id}/cancel`));
      await this.refreshOrders();
      await this.refreshManagerAnalytics();
      this.notify('Order cancelled');
    } catch (error) {
      this.handleError(error);
    }
  }

  async advanceOrder(id: number, status: 'Paid' | 'Shipped' | 'Delivered'): Promise<void> {
    this.apiError.set('');
    try {
      await firstValueFrom(this.api.put<OrderDto>(`/orders/${id}/status?status=${status}`));
      await this.refreshOrders();
      await this.refreshManagerAnalytics();
      this.notify('Order status updated');
    } catch (error) {
      this.handleError(error);
    }
  }

  async createProduct(
    request: Omit<
      ProductDto,
      'id' | 'effectivePrice' | 'images' | 'createdAtUtc' | 'averageRating' | 'reviewCount'
    >,
    image?: File | null,
  ): Promise<void> {
    this.apiError.set('');
    try {
      const product = await firstValueFrom(this.api.post<ProductDto>('/products', request));
      if (image) {
        try {
          await firstValueFrom(this.api.upload(`/products/${product.id}/images`, image));
        } catch (error) {
          await this.loadManagerProducts();
          this.handleError(
            new Error(
              `Product saved, but its image could not be uploaded. ${this.errorMessage(error)}`,
            ),
          );
          return;
        }
      }
      await this.loadManagerProducts();
      this.notify('Product added to your catalog');
    } catch (error) {
      this.handleError(error);
    }
  }

  async uploadProductImage(productId: number, file: File): Promise<boolean> {
    this.apiError.set('');
    try {
      await firstValueFrom(this.api.upload<ProductImageDto>(`/products/${productId}/images`, file));
      await this.loadManagerProducts();
      this.notify('Product photo uploaded');
      return true;
    } catch (error) {
      this.handleError(error);
      return false;
    }
  }

  async reorderProductImages(productId: number, imageIds: number[]): Promise<boolean> {
    this.apiError.set('');
    try {
      await firstValueFrom(
        this.api.put<ProductImageDto[]>(`/products/${productId}/images/order`, { imageIds }),
      );
      await this.loadManagerProducts();
      this.notify('Product photos reordered');
      return true;
    } catch (error) {
      this.handleError(error);
      return false;
    }
  }

  async deleteProductImage(productId: number, imageId: number): Promise<void> {
    this.apiError.set('');
    try {
      await firstValueFrom(this.api.delete<unknown>(`/products/${productId}/images/${imageId}`));
      await this.loadManagerProducts();
      this.notify('Product photo removed');
    } catch (error) {
      this.handleError(error);
    }
  }

  async updateProduct(
    id: number,
    request: Omit<
      ProductDto,
      'id' | 'effectivePrice' | 'images' | 'createdAtUtc' | 'averageRating' | 'reviewCount'
    >,
    image?: File | null,
  ): Promise<void> {
    this.apiError.set('');
    try {
      await firstValueFrom(this.api.put<ProductDto>(`/products/${id}`, request));
      if (image) {
        try {
          await firstValueFrom(this.api.upload(`/products/${id}/images`, image));
        } catch (error) {
          await this.loadManagerProducts();
          this.handleError(
            new Error(
              `Product saved, but its image could not be uploaded. ${this.errorMessage(error)}`,
            ),
          );
          return;
        }
      }
      await this.loadManagerProducts();
      this.notify('Product updated');
    } catch (error) {
      this.handleError(error);
    }
  }

  async deleteProduct(id: number): Promise<void> {
    this.apiError.set('');
    try {
      await firstValueFrom(this.api.delete<unknown>(`/products/${id}`));
      this.products.update((items) => items.filter((item) => item.id !== id));
      this.productTotalCount.update((count) => Math.max(0, count - 1));
      if (this.selectedProductId() === id) this.selectedProductId.set(null);
      this.notify('Product deleted from your catalog');
    } catch (error) {
      this.handleError(error);
    }
  }

  async createStore(name: string, description: string): Promise<void> {
    this.apiError.set('');
    try {
      const store = await firstValueFrom(this.api.post<StoreDto>('/stores', { name, description }));
      this.myStores.update((stores) => [...stores, store]);
      this.selectedStoreId.set(store.id);
      this.notify('Store created');
    } catch (error) {
      this.handleError(error);
    }
  }

  async createCampaign(
    request: Omit<MarketingCampaignDto, 'id' | 'isCurrentlyActive'>,
  ): Promise<void> {
    this.apiError.set('');
    try {
      const created = await firstValueFrom(
        this.api.post<MarketingCampaignDto>('/store-manager/campaigns', request),
      );
      this.campaigns.update((items) => [...items, created]);
      this.notify('Campaign created');
    } catch (error) {
      this.handleError(error);
    }
  }

  async updateCampaign(
    campaign: MarketingCampaignDto,
    request: Omit<MarketingCampaignDto, 'id' | 'isCurrentlyActive'>,
  ): Promise<void> {
    this.apiError.set('');
    try {
      const updated = await firstValueFrom(
        this.api.put<MarketingCampaignDto>(`/store-manager/campaigns/${campaign.id}`, request),
      );
      this.campaigns.update((items) =>
        items.map((item) => (item.id === campaign.id ? updated : item)),
      );
      this.notify('Campaign updated');
    } catch (error) {
      this.handleError(error);
    }
  }

  async deleteCampaign(id: number): Promise<void> {
    this.apiError.set('');
    try {
      await firstValueFrom(this.api.delete<unknown>(`/store-manager/campaigns/${id}`));
      this.campaigns.update((items) => items.filter((item) => item.id !== id));
      this.notify('Campaign deleted');
    } catch (error) {
      this.handleError(error);
    }
  }

  async createCategory(request: Omit<CategoryDto, 'id' | 'children' | 'products'>): Promise<void> {
    this.apiError.set('');
    try {
      await firstValueFrom(this.api.post<CategoryDto>('/categories', request));
      await this.refreshCategories();
      this.notify('Category created');
    } catch (error) {
      this.handleError(error);
    }
  }

  async updateCategory(
    category: CategoryDto,
    request: Omit<CategoryDto, 'id' | 'children' | 'products'>,
  ): Promise<void> {
    this.apiError.set('');
    try {
      await firstValueFrom(this.api.put<CategoryDto>(`/categories/${category.id}`, request));
      await this.refreshCategories();
      this.notify('Category updated');
    } catch (error) {
      this.handleError(error);
    }
  }

  async deleteCategory(id: number): Promise<void> {
    this.apiError.set('');
    try {
      await firstValueFrom(this.api.delete<unknown>(`/categories/${id}`));
      await this.refreshCategories();
      this.notify('Category deleted');
    } catch (error) {
      this.handleError(error);
    }
  }

  async deleteStore(id: number): Promise<void> {
    this.apiError.set('');
    try {
      await firstValueFrom(this.api.delete<unknown>(`/stores/${id}`));
      this.stores.update((items) => items.filter((item) => item.id !== id));
      this.myStores.update((items) => items.filter((item) => item.id !== id));
      this.products.update((items) => items.filter((item) => item.storeId !== id));
      this.notify('Store deleted');
    } catch (error) {
      this.handleError(error);
    }
  }

  async sendManagerMessage(storeId: number, message: string): Promise<void> {
    this.apiError.set('');
    try {
      await firstValueFrom(this.api.post<unknown>(`/stores/${storeId}/messages`, { message }));
      this.notify('Message sent to store manager');
    } catch (error) {
      this.handleError(error);
    }
  }

  async markNotificationRead(id: number): Promise<void> {
    try {
      const updated = await firstValueFrom(
        this.api.put<NotificationDto>(`/notifications/${id}/read`),
      );
      this.notifications.update((items) => items.map((item) => (item.id === id ? updated : item)));
    } catch (error) {
      this.handleError(error);
    }
  }

  setSection(section: WorkspaceSection): void {
    this.section.set(section);
  }
  setSearch(value: string): void {
    this.search.set(value);
  }
  setCategory(id: number): void {
    this.categoryId.set(id || null);
    this.subcategoryId.set(null);
  }
  setSubcategory(id: number): void {
    this.subcategoryId.set(id || null);
  }
  setBrandFilter(value: string): void {
    this.brandFilter.set(value);
  }
  setMinPrice(value: number | null): void {
    this.minPrice.set(value);
  }
  setMaxPrice(value: number | null): void {
    this.maxPrice.set(value);
  }
  clearProductFilters(): void {
    this.categoryId.set(null);
    this.subcategoryId.set(null);
    this.brandFilter.set('');
    this.minPrice.set(null);
    this.maxPrice.set(null);
  }
  categoryProductCount(categoryId: number): number {
    const root = this.allCategories().find((category) => category.id === categoryId);
    if (!root) return this.products().filter((product) => product.categoryId === categoryId).length;
    const ids = new Set<number>();
    const visit = (category: CategoryDto) => {
      ids.add(category.id);
      category.children?.forEach(visit);
    };
    visit(root);
    return this.products().filter((product) => ids.has(product.categoryId)).length;
  }
  setCartOpen(value: boolean): void {
    this.cartOpen.set(value);
  }
  setSelectedStore(id: number): void {
    this.selectedStoreId.set(id);
    void this.loadManagerProducts();
  }
  clearPrivateState(): void {
    this.role.set(null);
    this.currentUser.set(null);
    this.orders.set([]);
    this.cart.set(null);
    this.wishlist.set([]);
    this.notifications.set([]);
    this.myStores.set([]);
    this.users.set([]);
    this.campaigns.set([]);
    this.analytics.set(null);
    this.addresses.set([]);
    this.selectedStoreId.set(null);
    this.customerStoreId.set(null);
    this.selectedProductId.set(null);
    this.categoryId.set(null);
    this.subcategoryId.set(null);
    this.brandFilter.set('');
    this.minPrice.set(null);
    this.maxPrice.set(null);
    this.search.set('');
  }
  notify(message: string): void {
    this.toast.set(message);
    if (this.toastTimer) clearTimeout(this.toastTimer);
    this.toastTimer = setTimeout(() => this.toast.set(''), 3200);
  }

  handleError(error: unknown): void {
    const message = this.errorMessage(error);
    this.apiError.set(message);
    this.notify(message);
  }

  private errorMessage(error: unknown): string {
    return error instanceof Error ? error.message : 'The request could not be completed.';
  }
}
