import {Injectable} from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class UploadDownloadConfiguration {
  enableUpload(): boolean {
    return false;
  }

  enableDownload(): boolean {
    return false;
  }
}
