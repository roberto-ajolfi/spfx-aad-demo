import { WebPartContext } from "@microsoft/sp-webpart-base";

export interface IAadTestProps {
  description: string;
  apiUrl: string;
  clientId: string;
  context: WebPartContext;
}
