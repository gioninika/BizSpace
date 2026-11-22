import { Signal, WritableSignal, inject } from '@angular/core';
import {
  AppStateService,
  ProductReviewDto,
  ProductView,
  SavedAddressDto,
} from '../../services/app-state.service';
import { AuthSessionService } from '../../services/auth-session.service';
import { BizSpacePageComponent } from '../bizspace-page/bizspace-page.component';

export interface BizSpacePageUi {
  readonly profileBusy: WritableSignal<boolean>;
  readonly profileError: WritableSignal<string>;
  readonly passwordBusy: WritableSignal<boolean>;
  readonly passwordError: WritableSignal<string>;
  readonly addressEditorOpen: WritableSignal<boolean>;
  readonly addressBusy: WritableSignal<boolean>;
  readonly editingAddress: WritableSignal<SavedAddressDto | null>;
  readonly contactFeedback: WritableSignal<string>;
  readonly selectedProductImageUrl: WritableSignal<string | null>;
  readonly productReviews: WritableSignal<ProductReviewDto[]>;
  readonly reviewsLoading: WritableSignal<boolean>;
  readonly reviewsError: WritableSignal<string>;
  readonly customerReviewStarFilter: WritableSignal<number | null>;
  readonly filteredProductReviews: Signal<ProductReviewDto[]>;
  readonly reviewFormOpen: WritableSignal<boolean>;
  readonly reviewFormError: WritableSignal<string>;
  readonly reviewSubmitting: WritableSignal<boolean>;

  chooseSection(section: string): void;
  categoryIconType(name: string): string;
  categoryIcon(name: string): string;
  categoryPath(categoryId: number): string;
  openProduct(productId: number): void;
  closeProduct(): void;
  isSaved(productId: number): boolean;
  productCanBeAdded(product: ProductView): boolean;
  toggleSaved(productId: number): Promise<void>;
  addToCart(productId: number): Promise<void>;
  formatMoney(value: number): string;
  openCustomerStore(storeId: number): void;
  openAuth(mode?: 'login' | 'register'): void;
  formatDate(value: string): string;
  isOrderExpanded(orderId: number): boolean;
  toggleOrderDetails(orderId: number): void;
  statusClass(status: string): string;
  displayStatus(status: string): string;
  canCancelOrder(status: string): boolean;
  cancelOrder(id: number): Promise<void>;
  customerName(userId: number): string;
  customerEmail(userId: number): string;
  orderAddress(order: { deliveryMethod: string; shippingAddress: { streetAddress: string; city: string; postalCode: string; country: string } }): string;
  orderItemImage(item: { productId: number | null; imageUrl?: string | null }): string | null;
  saveProfile(form: HTMLFormElement): Promise<void>;
  changePassword(form: HTMLFormElement): Promise<void>;
  openAddressEditor(address?: SavedAddressDto): void;
  closeAddressEditor(): void;
  saveAddress(form: HTMLFormElement): Promise<void>;
  deleteSavedAddress(address: SavedAddressDto): Promise<void>;
  contactEmail(): string;
  contactPhone(): string;
  contactPhoneHref(): string;
  facebookUrl(): string;
  instagramUrl(): string;
  hasContactDetails(): boolean;
  submitContactMessage(form: HTMLFormElement): void;
  openContactMessage(form: HTMLFormElement, provider: 'gmail' | 'outlook' | 'default'): void;
  orderedProductImages(product: ProductView): ProductView['images'];
  selectProductImage(url: string): void;
  openProductReviews(productId: number): void;
  currentUserHasReviewed(): boolean;
  openReviewForm(): void;
  submitProductReview(productId: number, form: HTMLFormElement): Promise<void>;
  returnToProduct(productId: number): void;
  writeReviewFromList(productId: number): Promise<void>;
  retryProductReviews(productId: number): void;
  setCustomerReviewStarFilter(value: string): void;
  reviewStars(rating: number): string;
}

export abstract class CustomerRoutePage {
  protected readonly ui = inject(BizSpacePageComponent) as unknown as BizSpacePageUi;
  protected readonly state = inject(AppStateService);
  protected readonly auth = inject(AuthSessionService);
  protected readonly starOptions = [5, 4, 3, 2, 1];
}
