export const SESSION_KEYS_OCRT = {
  SELECTED_PROVIDER_ID: 'selectedProviderId',
  PROVIDER_DATA: 'providerData',
  END_DATE: 'endDate',
  SCHEDULE_WORKSHEETS: 'scheduleWorksheets',
  SELECTED_COST_REPORT: 'selectedCostReport',
  SELECTION_EXPAND_COLLAPSE: 'selectionExpandCollapse',
  SCHED_EXPAND_COLLAPSE: 'scheduleExpand/Collapse',
  USE_LOCAL_CALCULATIONS: 'useLocalCalculations',
  COST_REPORT_START_DATES_4_1: 'costReportStartDates_4_1',
  COST_REPORT_TYPE_LIST: 'costReportTypeList',
  SELECTED_TAB: 'selectedTab',
  SELECTED_SCHEDULE: 'selectedSchedule',
  SCHED_LAST: 'scheduleLastVisited',
  GRID_DEFINITION: 'gridDefinitionCache',
  SCHEDULE_VALUES_UPDATED: 'scheduleValuesUpdated',
  SCHEDULE_EDITS_REQUIRED: 'scheduleEditsRequired',
  SCHED_EDITS_CURRENT: 'currentScheduleEdits',
  CHART_OF_ACCTS_DROP_DOWN: 'chartOfAccountsDropDown',
  SCHEDULE_GRID_MASK: 'scheduleGridMask',
  SCHED_PAGE_COUNTER: 'schedulePageCounter',
  SCHED_PAGE_COUNT_FIRST: 'schedulePageCountFirst',
  SCHED_PAGE_COUNT_LAST: 'schedulePageCountLast',
} as const;

// Optional: Create a union type of all the values for strict typing in services/functions
export type SessionKeyOcrt = typeof SESSION_KEYS_OCRT[keyof typeof SESSION_KEYS_OCRT];
