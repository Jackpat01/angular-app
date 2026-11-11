import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AcceptTerms } from './accept-terms';

describe('AcceptTerms', () => {
  let component: AcceptTerms;
  let fixture: ComponentFixture<AcceptTerms>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AcceptTerms],
    }).compileComponents();

    fixture = TestBed.createComponent(AcceptTerms);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
