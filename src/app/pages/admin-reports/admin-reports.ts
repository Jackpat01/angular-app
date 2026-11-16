import {Component, OnInit, inject} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormBuilder, FormGroup, ReactiveFormsModule, Validators} from '@angular/forms';
import {BaseFormComponent} from '../../shared/base-form.component';

interface Report {
  id: number;
  name: string;
  type: string;
  generatedBy: string;
  generatedDate: string;
  status: 'completed' | 'processing' | 'failed';
  fileSize: string;
}

@Component({
  imports: [CommonModule, ReactiveFormsModule],
  selector: 'ohfi-admin-reports',
  styleUrl: './admin-reports.css',
  templateUrl: './admin-reports.html',
})
export class AdminReportsComponent extends BaseFormComponent implements OnInit {
  reports: Report[] = [
    {
      id: 1,
      name: 'User Activity Report',
      type: 'User Activity',
      generatedBy: 'john.doe@example.com',
      generatedDate: '2024-11-09',
      status: 'completed',
      fileSize: '2.4 MB',
    },
    {
      id: 2,
      name: 'System Performance Report',
      type: 'Performance',
      generatedBy: 'admin@example.com',
      generatedDate: '2024-11-08',
      status: 'completed',
      fileSize: '1.8 MB',
    },
    {
      id: 3,
      name: 'Error Log Report',
      type: 'Error Logs',
      generatedBy: 'jane.smith@example.com',
      generatedDate: '2024-11-08',
      status: 'processing',
      fileSize: 'N/A',
    },
    {
      id: 4,
      name: 'Database Report',
      type: 'Database',
      generatedBy: 'admin@example.com',
      generatedDate: '2024-11-07',
      status: 'failed',
      fileSize: 'N/A',
    },
  ];

  filteredReports: Report[] = [];
  showForm = false;
  filterType = 'all';

  protected getPageTitle(): string {
    return 'Reports Management';
  }

  protected buildForm(): FormGroup {
    return this.fb.group({
      reportType: ['User Activity'],
      startDate: [''],
      endDate: [''],
    });
  }

  override ngOnInit(): void {
    super.ngOnInit();
    this.filteredReports = this.reports;
  }

  filterReports(): void {
    if (this.filterType === 'all') {
      this.filteredReports = this.reports;
    } else {
      this.filteredReports = this.reports.filter(r => r.status === this.filterType);
    }
  }

  toggleForm(): void {
    this.showForm = !this.showForm;
    if (!this.showForm) {
      this.form.reset();
    }
  }

  generateReport(): void {
    if (this.form.valid) {
      alert('Report generation started. You will be notified when it completes.');
      this.showForm = false;
    }
  }

  downloadReport(id: number): void {
    const report = this.reports.find(r => r.id === id);
    if (report && report.status === 'completed') {
      alert(`Downloading: ${report.name}`);
    }
  }

  deleteReport(id: number): void {
    if (confirm('Are you sure you want to delete this report?')) {
      this.reports = this.reports.filter(r => r.id !== id);
      this.filteredReports = this.filteredReports.filter(r => r.id !== id);
    }
  }

  getStatusColor(status: string): string {
    switch (status) {
      case 'completed':
        return 'success';
      case 'processing':
        return 'warning';
      case 'failed':
        return 'danger';
      default:
        return 'secondary';
    }
  }
}
