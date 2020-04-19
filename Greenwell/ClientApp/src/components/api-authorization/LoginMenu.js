import React, { Component, Fragment } from 'react';
import { NavItem, NavLink } from 'reactstrap';
import { Link } from 'react-router-dom';
import authService from './AuthorizeService';
import { ApplicationPaths } from './ApiAuthorizationConstants';

export class LoginMenu extends Component {
    constructor(props) {
        super(props);

        this.state = {
            isAuthenticated: false,
            userName: null,
            role: null
        };
    }

    componentDidMount() {
        this._subscription = authService.subscribe(() => this.populateState());
        this.populateState();
    }

    componentWillUnmount() {
        authService.unsubscribe(this._subscription);
    }

    async populateState() {
        const [isAuthenticated, user] = await Promise.all([authService.isAuthenticated(), authService.getUser()])
        this.setState({
            isAuthenticated,
            userName: user && user.name,
            role: user && user.role
        });
    }

    render() {
        const { isAuthenticated, userName, role } = this.state;
        if (!isAuthenticated) {
            const loginPath = `${ApplicationPaths.Login}`;
            return this.anonymousView(loginPath);
        } else {
            const profilePath = `${ApplicationPaths.Profile}`;
            const logoutPath = { pathname: `${ApplicationPaths.LogOut}`, state: { local: true } };
            return this.authenticatedView(userName, profilePath, logoutPath);
        }
    }

    authenticatedView(userName, profilePath, logoutPath) {
        if (this.state.role != null) {
            if (this.state.role == "Administrator") {
                return (<Fragment>
                    <NavItem>
                        <NavLink tag={Link} className="text-dark" to={profilePath}>Hello {userName}</NavLink>
                    </NavItem>
                    <NavItem>
                        <NavLink tag={Link} className="text-dark" to={logoutPath}>Logout</NavLink>
                    </NavItem>
                    <NavItem>
                        <NavLink tag={Link} className="text-dark" to="/Admin/Manage">Admin Only/Handle Users</NavLink>
                    </NavItem>
                </Fragment>);
            }
            else {
                return (<Fragment>
                    <NavItem>
                        <NavLink tag={Link} className="text-dark" to={profilePath}>Hello {userName} | Employee</NavLink>
                    </NavItem>
                    <NavItem>
                        <NavLink tag={Link} className="text-dark" to={logoutPath}>Logout</NavLink>
                    </NavItem>
                </Fragment>);
            }
        }
    }

    anonymousView(loginPath) {
        return (<Fragment>
            <NavItem>
                <NavLink tag={Link} className="text-dark" to={loginPath}>Login</NavLink>
            </NavItem>
        </Fragment>);
    }
}
