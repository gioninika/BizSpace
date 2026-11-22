import { Component, Input, inject } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { AppStateService, ProductView } from '../../services/app-state.service';
import { BizSpacePageComponent } from '../../pages/bizspace-page/bizspace-page.component';
import { BizSpacePageUi } from '../../pages/shared/customer-route-page';

@Component({
  selector: 'app-product-grid',
  standalone: true,
  imports: [DecimalPipe],
  templateUrl: './product-grid.component.html',
  styleUrl: './product-grid.component.scss',
})
export class ProductGridComponent {
  @Input() products: ProductView[] = [];
  @Input() emptyTitle = 'Nothing found just yet.';
  @Input() emptyCopy = 'Try another search or browse a different category.';
  protected readonly state = inject(AppStateService);
  protected readonly ui = inject(BizSpacePageComponent) as unknown as BizSpacePageUi;
}
