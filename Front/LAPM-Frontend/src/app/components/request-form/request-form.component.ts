import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, AsyncValidatorFn, FormBuilder, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Observable, of, timer } from 'rxjs';
import { map, switchMap } from 'rxjs/operators';
import { RequestService } from '../../core/services/request.service';

@Component({
  selector: 'app-request-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './request-form.component.html',
  styleUrls: ['./request-form.component.css']
})
export class RequestFormComponent implements OnInit {
  requestForm!: FormGroup;
  submissionStatus: 'idle' | 'success' | 'error' = 'idle';
  errorMessage: string = '';

  constructor(
    private fb: FormBuilder,
    private requestService: RequestService
  ) {}

  ngOnInit(): void {
    this.requestForm = this.fb.group({
      computerName: ['', [Validators.required], [this.computerExistsValidator()]],
      domainUser: ['', [Validators.required], [this.userExistsValidator()]],
      expirationTime: ['', [Validators.required, this.futureDateValidator()]],
      notes: ['']
    });
  }

  onSubmit(): void {
    if (this.requestForm.valid) {
      this.requestService.createRequest(this.requestForm.value).subscribe({
        next: () => {
          this.submissionStatus = 'success';
          this.requestForm.reset();
        },
        error: (err) => {
          this.submissionStatus = 'error';
          this.errorMessage = err.error?.message || 'An unknown error occurred.';
        }
      });
    }
  }

  // --- Custom Validators ---
  private futureDateValidator(): (control: AbstractControl) => ValidationErrors | null {
    return (control: AbstractControl): ValidationErrors | null => {
        if (!control.value) { return null; }
        return new Date(control.value) > new Date() ? null : { pastDate: true };
    };
  }

  // --- Async Validators ---
  private computerExistsValidator(): AsyncValidatorFn {
    return (control: AbstractControl): Observable<ValidationErrors | null> => {
      if (!control.value) { return of(null); }
      return timer(500).pipe( // Debounce time
        switchMap(() => this.requestService.checkComputerExists(control.value)),
        map(exists => (exists ? null : { computerNotFound: true }))
      );
    };
  }

  private userExistsValidator(): AsyncValidatorFn {
    return (control: AbstractControl): Observable<ValidationErrors | null> => {
      if (!control.value) { return of(null); }
      return timer(500).pipe( // Debounce time
        switchMap(() => this.requestService.checkUserExists(control.value)),
        map(exists => (exists ? null : { userNotFound: true }))
      );
    };
  }
}
