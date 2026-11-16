import {Component, OnInit} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormBuilder, FormGroup, ReactiveFormsModule} from '@angular/forms';
import {BaseListCrudComponent} from '../../shared/base-list-crud.component';

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
export class AdminReportsComponent extends BaseListCrudComponent<Report> implements OnInit {
  filterType = 'all';

  constructor() {
    super();
    this.items = [
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
  }

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

  protected matchesSearchTerm(item: Report): boolean {
    const term = this.searchTerm.toLowerCase();
    return item.name.toLowerCase().includes(term) || 
           item.type.toLowerCase().includes(term) ||
           item.generatedBy.toLowerCase().includes(term);
  }

  protected onItemDeleted(id: number | string): void {
    // Placeholder for post-delete logic (API call, notification, etc)
  }

  ngOnInit(): void {
    this.loadItems();
  }

  protected loadItems(): void {
    this.applyFilter();
  }

  filterReportsByStatus(): void {
    if (this.filterType === 'all') {
      this.applyFilter();
    } else {
      this.filteredItems = this.items.filter(r => r.status === this.filterType);
    }
  }

  generateReport(): void {
    if (this.form.valid) {
      alert('Report generation started. You will be notified when it completes.');
      this.showForm = false;
    }
  }

  downloadReport(id: number): void {
    const report = this.items.find(r => r.id === id);
    if (report && report.status === 'completed') {
      alert(`Downloading: ${report.name}`);
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

