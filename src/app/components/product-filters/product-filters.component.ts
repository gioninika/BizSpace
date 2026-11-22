import { Component, inject } from '@angular/core';
import { AppStateService } from '../../services/app-state.service';
import { BizSpacePageComponent } from '../../pages/bizspace-page/bizspace-page.component';
import { BizSpacePageUi } from '../../pages/shared/customer-route-page';

@Component({
  selector: 'app-product-filters',
  standalone: true,
  templateUrl: './product-filters.component.html',
  styleUrl: './product-filters.component.scss',
})
export class ProductFiltersComponent {
  protected readonly state = inject(AppStateService);
  protected readonly ui = inject(BizSpacePageComponent) as unknown as BizSpacePageUi;
}
