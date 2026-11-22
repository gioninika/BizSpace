import { Component, EventEmitter, Output, computed, inject, signal } from '@angular/core';
import { AppStateService } from '../../services/app-state.service';
import { AuthSessionService } from '../../services/auth-session.service';

type AuthMode = 'login' | 'register';

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
  standalone: true,
})
export class HeaderComponent {
  protected readonly state = inject(AppStateService);
  protected readonly auth = inject(AuthSessionService);
  protected readonly accountMenuOpen = signal(false);
  protected readonly notificationPanelOpen = signal(false);
  protected readonly notificationUnreadOnly = signal(false);
  protected readonly roleLabel = computed(() =>
    this.auth.role() === 'StoreManager' ? 'Store manager' : (this.auth.role() ?? 'Customer'),
  );
  protected readonly customerNav = [
    'Discover',
    'Shop',
    'Stores',
    'About us',
    'Contact us',
    'My orders',
    'Wishlist',
  ];
  protected readonly visibleNotifications = computed(() =>
    this.notificationUnreadOnly()
      ? this.state.notifications().filter((item) => !item.isRead)
      : this.state.notifications(),
  );

  @Output() readonly sectionSelected = new EventEmitter<string>();
  @Output() readonly authRequested = new EventEmitter<AuthMode>();
  @Output() readonly logoutRequested = new EventEmitter<void>();
  @Output() readonly cartRequested = new EventEmitter<void>();

  protected requestSection(section: string): void {
    this.accountMenuOpen.set(false);
    this.sectionSelected.emit(section);
  }
  protected requestAuth(mode: AuthMode): void {
    this.authRequested.emit(mode);
  }
  protected requestLogout(): void {
    this.accountMenuOpen.set(false);
    this.notificationPanelOpen.set(false);
    this.notificationUnreadOnly.set(false);
    this.logoutRequested.emit();
  }
  protected requestCart(): void {
    this.cartRequested.emit();
  }
  protected isNavActive(section: string): boolean {
    return this.state.section() === section;
  }
  protected prettyRole(): string {
    return this.roleLabel();
  }
  protected formatDate(value: string): string {
    return value ? new Date(value).toLocaleDateString() : '—';
  }
  protected switchNotificationPanel(): void {
    this.notificationPanelOpen.update((open) => !open);
  }
  protected toggleUnreadOnlyNotifications(): void {
    this.notificationUnreadOnly.update((unreadOnly) => !unreadOnly);
  }
  protected toggleAccountMenu(): void {
    this.accountMenuOpen.update((open) => !open);
  }
  protected async readNotification(id: number): Promise<void> {
    await this.state.markNotificationRead(id);
  }
}
