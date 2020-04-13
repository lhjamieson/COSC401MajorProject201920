import React from 'react'
import { Component } from 'react'
import { Route, Redirect } from 'react-router-dom'
import { ApplicationPaths, QueryParameterNames } from './ApiAuthorizationConstants'
import authService from './AuthorizeService'

//Authorize admin route allows us to prevent employee user from accessing admin only resources.
export default class AuthorizeAdminRoute extends Component {
    constructor(props) {
        super(props);

        this.state = {
            ready: false,
            authenticated: false,
            admin: false
        };
    }

    componentDidMount() {
        this._subscription = authService.subscribe(() => this.authenticationChanged());
        this.populateAuthenticationState();
    }

    componentWillUnmount() {
        authService.unsubscribe(this._subscription);
    }

    //We check if they are logged in and an admin, if so we send them to the intended resource with a route
    //If not show them a you are unauthorized message.
    render() {
        const { ready, authenticated, admin } = this.state;
        if (!ready) {
            return <div></div>;
        } else {
            const { component: Component, ...rest } = this.props;
            return <Route {...rest}
                render={(props) => {
                    if (authenticated && admin) {
                        return <Component {...props} />
                    } else {
                        return <div>You are unauthorized to access this page.</div>
                    }
                }} />
        }
    }

    async populateAuthenticationState() {
        const authenticated = await authService.isAuthenticated();
        var admin = null;
        const user = await authService.getUser();
        if (user && user.role == "Administrator") 
            admin = true;
        else 
            admin = false;
        this.setState({ ready: true, authenticated, admin });
    }

    async authenticationChanged() {
        this.setState({ ready: false, authenticated: false, admin: false });
        await this.populateAuthenticationState();
    }
}
