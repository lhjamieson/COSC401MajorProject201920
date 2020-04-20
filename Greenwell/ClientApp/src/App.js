import React, { Component } from 'react';
import { Route } from 'react-router';
import { Layout } from './components/Layout';
import { Home } from './components/Home';
import { AdminOnly } from './components/AdminOnly';
import AuthorizeRoute from './components/api-authorization/AuthorizeRoute';
import AuthorizeAdminRoute from './components/api-authorization/AuthorizeAdminRoute';
import ApiAuthorizationRoutes from './components/api-authorization/ApiAuthorizationRoutes';
import { ApplicationPaths } from './components/api-authorization/ApiAuthorizationConstants';

import './custom.css'

export default class App extends Component {
  static displayName = App.name;

  render () {
    return (
        <Layout>
            {/*Switching to AuthorizeRoute like before forces the user to login before they see the homepage, 
             * however for testing of the homepage it can be tedious to login every time so for now it will remain an UnauthorizedRoute
             * <AuthorizeRoute exact path='/' component={Home} />*/}
            
            <AuthorizeRoute exact path='/' component={Home} />
            <AuthorizeAdminRoute exact path='/Admin/Manage' component={AdminOnly} />
        
        <Route path={ApplicationPaths.ApiAuthorizationPrefix} component={ApiAuthorizationRoutes} />
      </Layout>
    );
  }
}
