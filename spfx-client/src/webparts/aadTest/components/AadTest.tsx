import * as React from 'react';
import styles from './AadTest.module.scss';
import { IAadTestProps } from './IAadTestProps';
import { escape } from '@microsoft/sp-lodash-subset';
import { AadHttpClient, HttpClientResponse } from '@microsoft/sp-http';

export interface IAadTestState { 
  weatherData: any[];
  token: string;
  loading: boolean;
  graphData: any;
}

export default class AadTest extends React.Component<IAadTestProps, IAadTestState> {
  constructor(props: IAadTestProps) {
    super(props);

    this.state = { weatherData: [], token: '', loading: true, graphData: {} };
  }

  public async componentDidMount(): Promise<void> {
    const apiClient = await this.props.context.aadHttpClientFactory
      .getClient(this.props.clientId);

    const tokenSvc = await this.props.context.aadTokenProviderFactory.getTokenProvider();
    const token = await tokenSvc.getToken(this.props.clientId);

    const graphResults = await apiClient
      .get(`${this.props.apiUrl}/graph`, AadHttpClient.configurations.v1);

    const graphData = await graphResults.json();

    const results = await apiClient
      .get(this.props.apiUrl, AadHttpClient.configurations.v1);
    
    const weatherData = await results.json();

    this.setState({ weatherData, token, loading: false, graphData });
  } 

  public render(): React.ReactElement<IAadTestProps> {
    if(this.state.loading)
      return(<h1>Loading ...</h1>);

    const {displayName, employeeId, jobTitle} = this.state.graphData;
      
    return (
      <div className={ styles.aadTest }>
        <div className={ styles.container }>
          <div className={ styles.row }>
            <div className={ styles.column }>
              <span className={ styles.title }>Hello {displayName} [{employeeId}], Welcome to SharePoint!</span>
              <p className={ styles.subTitle }>Customize SharePoint experiences using Web Parts.</p>
              <p className={ styles.description }>
                <ul>
                  {this.state.weatherData.map(o => <li>[{o.date}] {o.summary}</li>)}
                </ul>
              </p>
              <p className={styles.token}>
                {this.state.token}
              </p>
              <a href="https://aka.ms/spfx" className={ styles.button }>
                <span className={ styles.label }>Learn more</span>
              </a>
            </div>
          </div>
        </div>
      </div>
    );
  }
}
