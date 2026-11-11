import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { NgbModule, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { TitleService } from '../../services/title';

@Component({
  imports: [CommonModule, ReactiveFormsModule, NgbModule],
  selector: 'ohfi-sample-form',
  styleUrl: './sample-form.css',
  templateUrl: './sample-form.html',
})
export class SampleForm implements OnInit {
  contactForm!: FormGroup;
  submitted = false;

  constructor(
    private fb: FormBuilder, 
    private titleService: TitleService,
    private modalService: NgbModal
  ) {}

  ngOnInit(): void {
    this.titleService.setTitle('Contact Form');
    this.initializeForm();
  }

  initializeForm(): void {
    this.contactForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.pattern(/^[0-9\-\+\(\)]{10,}$/)]],
      subject: ['', Validators.required],
      message: ['', [Validators.required, Validators.minLength(10)]],
      subscribe: [false]
    });
  }

  get f() {
    return this.contactForm.controls;
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
