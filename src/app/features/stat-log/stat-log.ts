import {Component, computed, effect, inject, input, OnInit, Signal, signal, WritableSignal} from '@angular/core';
import {TempDataService} from '../../core/services/temp-data';
import {Router, RouterLink} from '@angular/router';
import {DatePipe, NgOptimizedImage} from '@angular/common';
import {MatDialog} from '@angular/material/dialog';
import {ReactiveFormsModule} from '@angular/forms';
import {NgIcon, provideIcons} from '@ng-icons/core';
import {
  featherCheckCircle,
  featherZap,
  featherSlash,
  featherUploadCloud,
  featherDownloadCloud,
  featherFlag,
  featherInfo,
  featherCalendar,
  featherUser,
  featherRefreshCw,
  featherMail,
  featherEyeOff,
  featherEye,
  featherCloudOff,
  featherFileMinus
} from '@ng-icons/feather-icons';
import {SelectItem, SelectOption} from '../../core/models/SelectItem';
import {StatlogARUMConfigurationService} from '../../core/services/statlog-arumconfiguration-service';
import {Classification} from '../../core/models/Classification';
import {ARUMProxy_User} from '../../core/models/ARUMProxy_User';
import {StatLogConfigurationService} from '../../core/services/StatLogConfigurationService';
import {SessionStorageService} from '../../core/services/session-storage-service';

@Component({
  imports: [
    RouterLink,
    DatePipe,
    NgOptimizedImage,
    ReactiveFormsModule,
    NgIcon
  ],
  providers: [provideIcons({
    featherCheckCircle,
    featherZap,
    featherSlash,
    featherUploadCloud,
    featherDownloadCloud,
    featherFlag,
    featherInfo,
    featherCalendar,
    featherUser,
    featherRefreshCw,
    featherMail,
    featherEyeOff,
    featherEye,
    featherCloudOff,
    featherFileMinus
  })],
  selector: 'ohfi-stat-log',
  styleUrl: './stat-log.css',
  templateUrl: './stat-log.html',
})
export class StatLog implements OnInit {
  private session = inject(SessionStorageService);
  private tempData = inject(TempDataService);
  private statlogARUMConfig = inject(StatlogARUMConfigurationService);
  private statLogConfiguration = inject(StatLogConfigurationService);

  protected message = signal<string | null>(null);
  protected successMessageAttachmentUpload = signal<string | null>(null);
  protected customizeHeaderText = signal<string | null>(null);
  protected customizeBeginDateText = signal<string | null>(null);
  protected customizeEndDateText = signal<string | null>(null);
  protected beginningOfPeriod = signal<Date | null>(null);
  protected isActive = signal<boolean>(true);
  private readonly dialog = inject(MatDialog);
  protected sProviderID = input.required<string>();
  protected readOnly = input<boolean>(false);
  protected sCostReportID = signal<string | null>(null);
  protected myCostReportHistory = signal<HistoryDO[]>([]);
  protected myInteractiveEvents = signal<SelectOption []>([]);
  protected myCostReportsEndDateConcatAuditTypeDesc = signal<SelectOption []>([]);
  protected hideLegend = signal<boolean>(localStorage.getItem('hideLegend') === 'true');
  private canAccessDueDateUpdateSignal = signal<boolean>(false);
  private classificationSignal = signal<Classification | null>(null);
  private userSignal = signal<ARUMProxy_User | null>(null);
  private router = inject(Router);


  private featureAccessMap = new Map<string, WritableSignal<boolean>>([
    ['DueDateUpdate:Access', this.canAccessDueDateUpdateSignal],

  ]);

  get classification(): Signal<Classification | null> {
    return this.classificationSignal;
  }

  get user(): Signal<ARUMProxy_User | null> {
    return this.userSignal;
  }

  get canAccessDueDateUpdate(): boolean {
    return this.canAccessDueDateUpdateSignal();
  }

  get isProviderConsultant() {
    return this.session.isProviderConsultant
  }

  get isPHIControlsEnabled() {
    return this.statLogConfiguration.IsPHIControlsEnabled
  }

  get isNAEnabled() {
    return this.statLogConfiguration.IsNAEnabled
  }

  get canSendStatLogEmail() {
    return this.statLogConfiguration.CanSendStatLogEmail(this.session.ARUMUser)
  }

  get canCreateStatLog() {
    return this.statLogConfiguration.CanCreateStatLog(this.session.userRoles)
  }

  get showEffectiveDate() {
    return this.statlogARUMConfig.ShowEffectiveDate(this.session.classification)
  }

  constructor() {
    // Automatically sync changes to localStorage whenever the signal updates
    effect(() => {
      localStorage.setItem('hideLegend', String(this.hideLegend()));
    });
  }

  ngOnInit(): void {
    let enableFullLogging = this.statlogARUMConfig.enableFullStatLogControllerLogging(this.classification());
    if (enableFullLogging)
      Log.Information("StatLog/{Action} for {Classification} called by user {UserEmail}", "Index", this.classification()?.displayName, this.user()?.email);


    let statLogTime = performance.now();
    if (this.user()?.providers == null || this.user()?.providers.length != 0) {
      this.tempData.setTempData('message', {message: 'No Providers Assigned to Current User.  Contact Support.  Click the icon below.'});
      this.router.navigate(['/log']);
    }

    let identifierTime = performance.now();
    let primaryIdentifier: string = ProviderBL.GetPrimaryIdentifierDisplayName(this.classification());
    const duration = (performance.now() - identifierTime) / 1000;
    if (enableFullLogging)
      Log.Information("StatLog/{Action} primary identifier load for {Classification} took {Elapsed} s to run", "Index", this.classification()?.displayName, duration);


    let data = this.tempData.getTempData('arumProviderID');
    const trimmedProviderId = typeof data?.['arumProviderID'] === 'string' ? data['arumProviderID'] : null;

    let arumProviderID = trimmedProviderId !== '' ? Number(trimmedProviderId) : NaN;

    data = this.tempData.getTempData('costReportID');
    const trimmedCostReportId = typeof data?.['costReportID'] === 'string' ? data['costReportID'] : null;
    const costReportID = trimmedCostReportId !== '' ? Number(trimmedCostReportId) : NaN;

    let providerTime = performance.now();
    var providerList1 = ProviderBL.GetAllARUMProviderInfoByARUMUserID(this.classification(), this.user(), "Statlog:View");
    var providerList = providerList1.Select(p => new StatLogViewModel.ProviderDropDown
    {
      Name = p.Name,
        Number = p.Number,
        EffectiveDate = p.EffectiveDate,
        EffectiveDateString = p.EffectiveDate != null ? p.EffectiveDate.Value.ToString("MM/dd/yyyy") : '',
        ARUMProviderID = ToClientID(p.ARUMProviderID, PROVIDER_ID_STATLOG)
    }
  ).
    OrderBy(p => p.Name).ThenBy(p => p.Number).ThenBy(p => p.EffectiveDate).ToList();
    let durationProvider = (performance.now() - providerTime) / 1000;
    if (enableFullLogging)
      Log.Information("StatLog/{Action} provider load for {Classification} took {Elapsed} s to run", "Index", this.classification()?.displayName, durationProvider);

    let providerID: number = ToInternalID(sProviderID, PROVIDER_ID_STATLOG);
    var myViewModel = new StatLogViewModel.View
    {
      PageTitle = "Stat Log",
        myAutoProviderInformation = providerList,
        StatLogTable = new List<StatLogViewModel.StatLogRow>(),
        BeginningOfPeriod = null,
        EndOfPeriod = null,
        ContactID = 0, //Might store ARUM User ID here shortly.
        IsPHISafe = StatlogSession.IsPHISafe(providerID, this.session.ARUMUser),
        PrimaryIdentifier = !IsNullOrEmpty(primaryIdentifier) ? primaryIdentifier : "MCAID",
        readOnly = true
    }
    ;

    let newProviderID: number = ToInternalID(this.sProviderID, PROVIDER_ID_STATLOG);
    if (newProviderID != -1)
      arumProviderID = newProviderID;

    if (arumProviderID <= 0) {
      let durationStatLog = (performance.now() - statLogTime) / 1000;
      if (enableFullLogging)
        Log.Information("StatLog/{Action} loading for {Classification} took {Elapsed} s to run", "Index", this.classification()?.displayName, durationStatLog);
      return;
    }


    let map: Mapping = MapByProviderID(arumProviderID, "Statlog:View");
    if (map.Error) {
      this.tempData.setTempData('message', {message: map.ErrorMessage});
      return;
    }

    this.sProviderID = ToClientID(map.Provider.ARUMProviderID, PROVIDER_ID_STATLOG);
    this.PageTitle = map.Provider.Name;
    this.SelectedProviderName = map.Provider.Name;
    this.SelectedProviderIdentifier = map.Provider.Number;
    RetrieveSession.SelectedProviderID = map.Provider.ARUMProviderID;
    this.readOnly = !ARUMConfiguration.FeatureMatchesRole(this.classification(), "StatLog:Interact", this.session.ccurrentRoleName);

    let canHide: boolean = this.statLogConfiguration.canHide(  [this.session.currentRole] )
    ;

    let statlogDBTime = performance.now();
    var costReports = ProviderBL.GetStatLogs(this.classification(), arumProviderID)
      .Where(c => c.IsActive != false || canHide)
      .OrderByDescending(c => c.EndDate)
      .ToList();
    let durationStatLogDB = (performance.now() - statlogDBTime) / 1000;

    if (enableFullLogging)
      Log.Information("StatLog/{Action} retrieving statlogs for {Classification} provider {ARUMProviderID} took {Elapsed} s to run", "Index", this.classification()?.displayName, map.StatLog?.ARUMProviderID, durationStatLogDB);

    this.myCostReportsEndDateConcatAuditTypeDesc = computed<SelectOption[]>(() => {
      return this.costReports().map(m => {
        const endDateStr = m.endDate ? this.toShortDateString(m.endDate) : '';
        const beginDateStr = m.beginDate ? this.toShortDateString(m.beginDate) : '';

        return {
          text: `${endDateStr} - ${m.auditTypeDesc}`,
          value: `${this.toClientID(m.costReportID, COST_REPORT_ID_STATLOG)}|${beginDateStr}`
        };
      });
    });

    if (costReportID <= 0) {
      let durationStatLogTime = (performance.now() - statLogTime) / 1000;

      if (enableFullLogging)
        Log.Information("StatLog/{Action} loading for {Classification} provider {ARUMProviderID} took {Elapsed} s to run", "Index", this.classification()?.displayName, map.StatLog?.ARUMProviderID, durationStatLogTime);
      return;
    }

    map = MapByCostReportID(costReportID, "Statlog:View");
    if (map.Error) {
      this.tempData.setTempData('message', {message: map.ErrorMessage});
      return;
    }

    if (map.StatLog.IsActive == false && !this.statLogConfiguration.canHide(this.session.userRoles))
      return;

    if (map.Provider.ARUMProviderID != arumProviderID)
      return;

    this.sCostReportID = ToClientID(map.StatLog.CRID, COST_REPORT_ID_STATLOG);
    this.PageTitle = $
    "{map.Provider.Name} - {map.StatLog.EndDate.ToShortDateString()}";
    this.IsActive = map.StatLog.IsActive;
    this.BeginningOfPeriod = map.StatLog.BeginDate;
    this.EndOfPeriod = map.StatLog.EndDate;

    let historyTime = performance.now();
    this.myCostReportHistory = CostReportHistoryBL.GetHistoryByCostReportID(this.classification(), map.StatLog.CRID, this.user()?.userID, "StatLog", "Index", map.Provider.ARUMProviderID);
    let durationHistoryTime = (performance.now() - historyTime) / 1000;
    if (enableFullLogging)
      Log.Information("StatLog/{Action} history retrieval for {Classification} provider {ARUMProviderID} took {Elapsed} s to run", "Index", this.classification()?.displayName, map.StatLog?.ARUMProviderID, durationHistoryTime);

    this.myCostReportHistory?.Where(cr => !cr.UserName?.Contains("@") ?? true).ToList().ForEach(cr => cr.UserName = "SYSTEM");

    this.CostReportIDConcatBeginDate = $
    "{ToClientID(map.StatLog.CRID, COST_REPORT_ID_STATLOG)}|{map.StatLog.BeginDate.ToShortDateString()}";

    let interactiveTime = performance.now();
    var interactiveEvents = CostReportHistoryBL
      .GetInteractiveEvents(this.classification(), [this.session.currentRole], map.StatLog.CRID, true).
    OrderBy(e => e.Name)
      .Select(e => new SelectListItem
    {
      Text = $
      "{e.Name}{(IsNullOrWhiteSpace(e.Descript) ? Empty : " - " + e.Descript)}",
        Value = ToClientID(e.ID, EVENT_ID_STATLOG),
        Selected = false,
        Disabled = false,
    }
  )
  .
    ToList();
    let durationInteractiveTime = (performance.now() - interactiveTime) / 1000;
    if (enableFullLogging)
      Log.Information("StatLog/{Action} interactive event retrieval for {Classification} provider {ARUMProviderID} took {Elapsed} s to run", "Index", this.classification()?.displayName, map.StatLog?.ARUMProviderID, durationInteractiveTime);

    this.myInteractiveEvents = new SelectList(interactiveEvents, "Value", "Text");

    let actionTime = performance.now();
    GenerateActionItems.generateActionItems(myViewModel);
    let durationActionTime = (performance.now() - actionTime) / 1000;
    if (enableFullLogging)
      Log.Information("StatLog/{Action} generate action items for {Classification} provider {ARUMProviderID} took {Elapsed} s to run", "Index", this.classification()?.displayName, map.StatLog?.ARUMProviderID, durationActionTime);

    this.ContainsNAEvents = this.myCostReportHistory
      .Any(h => h.Event.ID == this.statLogConfiguration.EventIDForNA || h.Event.CanUseNA);

    let durationStatLogTime = (performance.now() - statLogTime) / 1000;
    if (enableFullLogging)
      Log.Information("StatLog/{Action} loading for {Classification} provider {ARUMProviderID}, cost report {CostReportID} took {Elapsed} s to run", "Index", this.classification()?.displayName, map.StatLog?.ARUMProviderID, map.StatLog.CRID, durationStatLogTime);


    //old
    const messageData = this.tempData.consume('message');
    const messageValue = typeof messageData?.['message'] === 'string' ? messageData['message'] : null;
    this.message.set(messageValue && messageValue.trim().length > 0 ? messageValue : null);

    const successData = this.tempData.consume('successMessageAttachmentUpload');
    const successValue = typeof successData?.['successMessageAttachmentUpload'] === 'string' ? successData['successMessageAttachmentUpload'] : null;
    this.successMessageAttachmentUpload.set(successValue && successValue.trim().length > 0 ? successValue : null);


  }

  openStatLogViaProvider(): void {
    if (this.readOnly()) return;

    this.dialog.open(CreateStatLogModalComponent, {
      width: '600px', // Equivalent to your 'medium' size
      data: {sArumProviderID: this.sProviderID()},
    });
  }

  openStatLogViaCostReport(): void {
    if (this.readOnly()) return;

    this.dialog.open(CreateStatLogModalComponent, {
      width: '600px', // Equivalent to your 'medium' size
      data: {sCostReportID: this.sCostReportID()},
    });
  }

  readonly downloadUrl = computed(() => {
    const url = new URL('/Transfer/DownloadFiles', environment.apiBaseUrl);
    url.searchParams.set('sCostReportID', this.sCostReportID() ?? '');
    return url.toString();
  });

  openDownloadModal(): void {
    this.modalService.open({
      url: this.downloadUrl(),
      width: '9080', // Or whatever '9080' represents in your modalButton helper
    });
  }

  toggleLegend(): void {
    this.hideLegend.update((hidden) => !hidden);
  }
}
