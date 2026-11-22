import { Routes } from '@angular/router';
import { BizSpacePageComponent } from './pages/bizspace-page/bizspace-page.component';

export const routes: Routes = [
  {
    path: '',
    component: BizSpacePageComponent,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'discover' },
      {
        path: 'discover',
        loadComponent: () =>
          import('./pages/discover/discover-page.component').then((page) => page.DiscoverPageComponent),
      },
      {
        path: 'products',
        loadComponent: () =>
          import('./pages/products/products-page.component').then((page) => page.ProductsPageComponent),
      },
      {
        path: 'stores/:storeId',
        loadComponent: () =>
          import('./pages/stores/stores-page.component').then((page) => page.StoresPageComponent),
      },
      {
        path: 'stores',
        loadComponent: () =>
          import('./pages/stores/stores-page.component').then((page) => page.StoresPageComponent),
      },
      {
        path: 'my-orders',
        loadComponent: () =>
          import('./pages/my-orders/my-orders-page.component').then((page) => page.MyOrdersPageComponent),
      },
      {
        path: 'wishlist',
        loadComponent: () =>
          import('./pages/wishlist/wishlist-page.component').then((page) => page.WishlistPageComponent),
      },
      {
        path: 'profile',
        loadComponent: () =>
          import('./pages/profile/profile-page.component').then((page) => page.ProfilePageComponent),
      },
      {
        path: 'address-book',
        loadComponent: () =>
          import('./pages/address-book/address-book-page.component').then((page) => page.AddressBookPageComponent),
      },
      {
        path: 'about',
        loadComponent: () =>
          import('./pages/about/about-page.component').then((page) => page.AboutPageComponent),
      },
      {
        path: 'contact',
        loadComponent: () =>
          import('./pages/contact/contact-page.component').then((page) => page.ContactPageComponent),
      },
      {
        path: 'product/:productId/reviews',
        loadComponent: () =>
          import('./pages/product-reviews/product-reviews-page.component').then((page) => page.ProductReviewsPageComponent),
      },
      {
        path: 'product/:productId',
        loadComponent: () =>
          import('./pages/product-detail/product-detail-page.component').then((page) => page.ProductDetailPageComponent),
      },
    ],
  },
  { path: 'manager/:section', component: BizSpacePageComponent },
  { path: 'admin/:section', component: BizSpacePageComponent },
  { path: '**', redirectTo: 'discover' },
];
