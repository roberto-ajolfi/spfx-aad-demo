import * as React from 'react';
import * as ReactDom from 'react-dom';
import { Version } from '@microsoft/sp-core-library';
import {
  IPropertyPaneConfiguration,
  PropertyPaneTextField
} from '@microsoft/sp-property-pane';
import { BaseClientSideWebPart, WebPartContext } from '@microsoft/sp-webpart-base';

import * as strings from 'AadTestWebPartStrings';
import AadTest from './components/AadTest';
import { IAadTestProps } from './components/IAadTestProps';

export interface IAadTestWebPartProps {
  description: string;
  apiUrl: string;
  clientId: string;
  context: WebPartContext;
}

export default class AadTestWebPart extends BaseClientSideWebPart<IAadTestWebPartProps> {

  public render(): void {
    const element: React.ReactElement<IAadTestProps> = React.createElement(
      AadTest,
      {
        description: this.properties.description,
        apiUrl: this.properties.apiUrl,
        clientId: this.properties.clientId,
        context: this.context
      }
    );

    ReactDom.render(element, this.domElement);
  }

  protected onDispose(): void {
    ReactDom.unmountComponentAtNode(this.domElement);
  }

  protected get dataVersion(): Version {
    return Version.parse('1.0');
  }

  protected getPropertyPaneConfiguration(): IPropertyPaneConfiguration {
    return {
      pages: [
        {
          header: {
            description: strings.PropertyPaneDescription
          },
          groups: [
            {
              groupName: strings.BasicGroupName,
              groupFields: [
                PropertyPaneTextField('description', {
                  label: strings.DescriptionFieldLabel
                }),
                PropertyPaneTextField('apiUrl', {
                  label: strings.ApiUrlFieldLabel
                }),
                PropertyPaneTextField('clientId', {
                  label: strings.ApiUrlFieldLabel
                })
              ]
            }
          ]
        }
      ]
    };
  }
}
