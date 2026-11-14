export const SESSION_KEYS_OCRT = {
  SELECTED_PROVIDER_ID: 'selectedProviderId',
  PROVIDER_DATA: 'ProviderData',
  END_DATE: 'EndDate',
  SCHEDULE_WORKSHEETS: 'ScheduleWorksheets',
  SELECTED_COST_REPORT: 'Selected Cost Report',
  SELECTION_EXPAND_COLLAPSE: 'Selection Expand/Collapse',
  SCHED_EXPAND_COLLAPSE: 'Schedule Expand/Collapse',
  USE_LOCAL_CALCULATIONS: 'UseLocalCalculations',
  COST_REPORT_START_DATES_4_1: 'CostReportStartDates_4_1',
  COST_REPORT_TYPE_LIST: 'CostReportTypeList',
  SELECTED_TAB: 'Selected Tab',
  SELECTED_SCHEDULE: 'Selected Schedule',
  SCHED_LAST: 'ScheduleLastVisited',
  GRID_DEFINITION: 'GridDefinitionCache',
  SCHEDULE_VALUES_UPDATED: 'ScheduleValuesUpdated',
  SCHEDULE_EDITS_REQUIRED: 'ScheduleEditsRequired',
  SCHED_EDITS_CURRENT: 'CurrentScheduleEdits',
  CHART_OF_ACCTS_DROP_DOWN: 'ChartOfAccountsDropDown',
  SCHEDULE_GRID_MASK: 'ScheduleGridMask',
  SCHED_PAGE_COUNTER: 'SchedulePageCounter',
  SCHED_PAGE_COUNT_FIRST: 'SchedulePageCountFirst',
  SCHED_PAGE_COUNT_LAST: 'SchedulePageCountLast',
} as const;

// Optional: Create a union type of all the values for strict typing in services/functions
export type SessionKeyOcrt = typeof SESSION_KEYS_OCRT[keyof typeof SESSION_KEYS_OCRT];
