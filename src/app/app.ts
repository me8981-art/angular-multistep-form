import { CommonModule } from '@angular/common';
import { Component, computed, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

@Component({
  selector: 'app-root',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly currentStep = signal(0);
  protected readonly submitted = signal(false);
  protected readonly steps = [
    { label: 'Your details', caption: 'Personal information' },
    { label: 'Preferences', caption: 'Tell us what you need' },
    { label: 'Review', caption: 'Confirm your request' }
  ];

  protected readonly form: FormGroup;

  constructor(private readonly formBuilder: FormBuilder) {
    this.form = this.formBuilder.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      company: [''],
      role: ['', Validators.required],
      projectType: ['', Validators.required],
      budget: ['', Validators.required],
      timeline: ['', Validators.required],
      notes: ['']
    });
  }

  protected readonly progress = computed(() => `${((this.currentStep() + 1) / this.steps.length) * 100}%`);


  protected next(): void {
    const controlsByStep = [
      ['firstName', 'lastName', 'email'],
      ['role', 'projectType', 'budget', 'timeline'],
      []
    ];
    const controls = controlsByStep[this.currentStep()];
    controls.forEach((name) => this.form.get(name)?.markAsTouched());

    if (controls.some((name) => this.form.get(name)?.invalid)) return;
    this.currentStep.update((step) => Math.min(step + 1, this.steps.length - 1));
  }

  protected back(): void {
    this.currentStep.update((step) => Math.max(step - 1, 0));
  }

  protected goToStep(index: number): void {
    if (index < this.currentStep()) this.currentStep.set(index);
  }

  protected submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.currentStep.set(0);
      return;
    }
    this.submitted.set(true);
  }

  protected isInvalid(name: string): boolean {
    const control = this.form.get(name);
    return !!control && control.invalid && control.touched;
  }
}
