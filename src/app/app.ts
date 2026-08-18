import { CommonModule } from '@angular/common';
import { Component, computed, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ProjectSubmission, SubmissionService, SubmissionStatus, TrackingResponse } from './submission.service';

@Component({
  selector: 'app-root',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly currentStep = signal(0);
  protected readonly submitted = signal(false);
  protected readonly submitting = signal(false);
  protected readonly submitError = signal('');
  protected readonly profilePicture = signal<File | null>(null);
  protected readonly attachments = signal<File[]>([]);
  protected readonly trackingId = signal('');
  protected readonly trackingQuery = signal('');
  protected readonly trackingResult = signal<TrackingResponse | null>(null);
  protected readonly trackingLoading = signal(false);
  protected readonly trackingError = signal('');
  protected readonly isAdminView = signal(false);
  protected readonly submissions = signal<ProjectSubmission[]>([]);
  protected readonly selectedSubmission = signal<ProjectSubmission | null>(null);
  protected readonly searchQuery = signal('');
  protected readonly statusFilter = signal<'All' | SubmissionStatus>('All');
  protected readonly adminLoading = signal(false);
  protected readonly adminError = signal('');
  protected readonly steps = [
    { label: 'Your details', caption: 'Personal information' },
    { label: 'Preferences', caption: 'Tell us what you need' },
    { label: 'Review', caption: 'Confirm your request' }
  ];

  protected readonly form: FormGroup;

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly submissionService: SubmissionService
  ) {
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
  protected readonly filteredSubmissions = computed(() => {
    const query = this.searchQuery().trim().toLowerCase();
    const status = this.statusFilter();
    return this.submissions().filter((submission) => {
      const matchesStatus = status === 'All' || submission.status === status;
      const haystack = `${submission.firstName} ${submission.lastName} ${submission.email} ${submission.company ?? ''} ${submission.projectType}`.toLowerCase();
      return matchesStatus && (!query || haystack.includes(query));
    });
  });
  protected readonly newCount = computed(() => this.submissions().filter((submission) => submission.status === 'New').length);
  protected readonly contactedCount = computed(() => this.submissions().filter((submission) => submission.status === 'Contacted').length);


  protected onProfilePictureChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.profilePicture.set(input.files?.[0] ?? null);
  }

  protected onAttachmentsChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.attachments.set(Array.from(input.files ?? []));
  }

  protected lookupTracking(): void {
    const query = this.trackingQuery().trim();
    if (!query) return;
    this.trackingLoading.set(true);
    this.trackingError.set('');
    this.trackingResult.set(null);
    this.submissionService.lookupTracking(query).subscribe({
      next: (result) => {
        this.trackingResult.set(result);
        this.trackingLoading.set(false);
      },
      error: () => {
        this.trackingLoading.set(false);
        this.trackingError.set('No submission was found for that tracking ID.');
      }
    });
  }

  protected toggleAdmin(): void {
    this.isAdminView.update((isAdmin) => !isAdmin);
    if (this.isAdminView()) this.loadSubmissions();
  }

  protected loadSubmissions(): void {
    this.adminLoading.set(true);
    this.adminError.set('');
    this.submissionService.listSubmissions().subscribe({
      next: (items) => {
        this.submissions.set(items);
        this.adminLoading.set(false);
      },
      error: () => {
        this.adminLoading.set(false);
        this.adminError.set('Could not load submissions. Make sure the API is running.');
      }
    });
  }

  protected openSubmission(submission: ProjectSubmission): void {
    this.selectedSubmission.set(submission);
  }

  protected closeSubmission(): void {
    this.selectedSubmission.set(null);
  }

  protected setStatus(status: SubmissionStatus): void {
    const submission = this.selectedSubmission();
    if (!submission) return;
    this.submissionService.updateStatus(submission.id, status).subscribe({
      next: (updated) => {
        this.submissions.update((items) => items.map((item) => item.id === updated.id ? updated : item));
        this.selectedSubmission.set(updated);
      },
      error: () => this.adminError.set('Could not update the submission status.')
    });
  }

  protected deleteSelected(): void {
    const submission = this.selectedSubmission();
    if (!submission) return;
    this.submissionService.deleteSubmission(submission.id).subscribe({
      next: () => {
        this.submissions.update((items) => items.filter((item) => item.id !== submission.id));
        this.closeSubmission();
      },
      error: () => this.adminError.set('Could not delete the submission.')
    });
  }

  protected formatDate(value: string): string {
    return new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric', year: 'numeric' }).format(new Date(value));
  }

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

    this.submitting.set(true);
    this.submitError.set('');
    this.submissionService.createSubmission({ ...this.form.getRawValue(), profilePicture: this.profilePicture(), attachments: this.attachments() }).subscribe({
      next: (response) => {
        this.submitting.set(false);
        this.trackingId.set(response.trackingId);
        this.submitted.set(true);
      },
      error: () => {
        this.submitting.set(false);
        this.submitError.set('We couldn’t save your request. Please try again.');
      }
    });
  }

  protected isInvalid(name: string): boolean {
    const control = this.form.get(name);
    return !!control && control.invalid && control.touched;
  }
}
