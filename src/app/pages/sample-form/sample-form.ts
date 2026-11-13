import {Component, OnInit, inject} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormBuilder, FormGroup, ReactiveFormsModule, Validators} from '@angular/forms';
import {NgbModal, NgbModule} from '@ng-bootstrap/ng-bootstrap';
import {TitleService} from '../../services/title';

@Component({
  imports: [CommonModule, ReactiveFormsModule, NgbModule],
  selector: 'ohfi-sample-form',
  styleUrl: './sample-form.css',
  templateUrl: './sample-form.html',
})
export class SampleForm implements OnInit {
  private fb = inject(FormBuilder);
  private titleService = inject(TitleService);
  private modalService = inject(NgbModal);
  contactForm!: FormGroup;
  submitted = false;

  get f() {
    return this.contactForm.controls;
  }

  ngOnInit(): void {
    this.titleService.setTitle('Contact Form');
    this.initializeForm();
  }

  initializeForm(): void {
    this.contactForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.pattern(/^[0-9\-+()]{10,}$/)]],
      subject: ['', Validators.required],
      message: ['', [Validators.required, Validators.minLength(10)]],
      subscribe: [false]
    });
  }

  onSubmit(): void {
    this.submitted = true;
    if (this.contactForm.valid) {
      console.log('Form Submitted:', this.contactForm.value);
      this.contactForm.reset();
      this.submitted = false;
    }
  }

  reset(): void {
    this.contactForm.reset();
    this.submitted = false;
  }
}
