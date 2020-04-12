import React, { Component } from 'react';
import { Table } from 'react-bootstrap';
import { Link } from 'react-router-dom';
import { Modal, Button } from 'react-bootstrap';
import authService from './api-authorization/AuthorizeService';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faSpinner, faTrash, faLevelUpAlt, faLevelDownAlt, faUserPlus } from '@fortawesome/free-solid-svg-icons';

export class AdminOnly extends Component {
    static displayName = AdminOnly.name;

    constructor(props) {
        super(props);
        this.state = {
            loading: true,
            adminUsers: null,
            nonAdminUsers: null,
            showDeleteUserModal: false,
            showMakeUserAdminModal: false,
            showMakeAdminNonAdminModal: false,
            showAddUserModal: false,
            userInAction: null,
        }


        let getUsers = async () => {
            const [user] = await Promise.all([authService.getUser()]);
            const token = await authService.getAccessToken();
            let formData = new FormData();
            formData.append("currentUser", user.name);
            const response = await fetch('api/AdminOnly/GetUsers', {
                method: 'POST',
                body: formData,
                headers: !token ? {} : { 'Authorization': `Bearer ${token}` }
            });
            const json = await response.json();
            this.setState({
                adminUsers: json.adminUsers,
                nonAdminUsers: json.nonAdminUsers,
                loading: false
            });
        }
        getUsers();
    }

    deleteAUser = async (userToDelete) => {
        const [user] = await Promise.all([authService.getUser()]);
        const token = await authService.getAccessToken();
        let formData = new FormData();
        formData.append("currentUser", user.name);
        formData.append("userToDelete", userToDelete);
        const response = await fetch('api/AdminOnly/DeleteUser', {
            method: 'POST',
            body: formData,
            headers: !token ? {} : { 'Authorization': `Bearer ${token}` }
        });
        const json = await response.json();
        this.setState({
            adminUsers: json.adminUsers,
            nonAdminUsers: json.nonAdminUsers,
            showDeleteUserModal: false,
            userInAction: null
        });
    };

    makeUserAdmin = async (userToMakeAdmin) => {
        const [user] = await Promise.all([authService.getUser()]);
        const token = await authService.getAccessToken();
        let formData = new FormData();
        formData.append("currentUser", user.name);
        formData.append("userToMakeAdmin", userToMakeAdmin);
        const response = await fetch('api/AdminOnly/MakeUserAdmin', {
            method: 'POST',
            body: formData,
            headers: !token ? {} : { 'Authorization': `Bearer ${token}` }
        });
        const json = await response.json();
        this.setState({
            adminUsers: json.adminUsers,
            nonAdminUsers: json.nonAdminUsers,
            showMakeUserAdminModal: false,
            userInAction: null
        });
    };

    makeUserNonAdmin = async (userToMakeNonAdmin) => {
        const [user] = await Promise.all([authService.getUser()]);
        const token = await authService.getAccessToken();
        let formData = new FormData();
        formData.append("currentUser", user.name);
        formData.append("userToMakeNonAdmin", userToMakeNonAdmin);
        const response = await fetch('api/AdminOnly/MakeUserNonAdmin', {
            method: 'POST',
            body: formData,
            headers: !token ? {} : { 'Authorization': `Bearer ${token}` }
        });
        const json = await response.json();
        this.setState({
            adminUsers: json.adminUsers,
            nonAdminUsers: json.nonAdminUsers,
            showMakeAdminNonAdminModal: false,
            userInAction: null
        });
    };

    validateEmail = (email) => {
        if (/^[a-zA-Z0-9]+@[a-zA-Z0-9]+\.[A-Za-z]+$/.test(email))
            return true;
        return false;
    }

    addUser = async (userToAdd) => {
        const [user] = await Promise.all([authService.getUser()]);
        const token = await authService.getAccessToken();
        let formData = new FormData();
        formData.append("currentUser", user.name);
        formData.append("userEmail", userToAdd);
        const response = await fetch('api/AdminOnly/AddUser', {
            method: 'POST',
            body: formData,
            headers: !token ? {} : { 'Authorization': `Bearer ${token}` }
        });
        const json = await response.json();
        this.setState({
            adminUsers: json.adminUsers,
            nonAdminUsers: json.nonAdminUsers,
            showMakeAdminNonAdminModal: false,
            userInAction: null
        }); 
    };

    render() {
        let content = (
            <div style={{ paddingLeft: "40px", marginTop: "20px" }}>
                <h1>Loading</h1>
                < FontAwesomeIcon className="fa-2x" icon={faSpinner} pulse />
            </div>
        );
        if (!this.state.loading) {
            content = (
                <React.Fragment>
                    <Modal show={this.state.showDeleteUserModal} onHide={() => this.setState({ showDeleteUserModal: false, userInAction: null })}>
                        <Modal.Header style={{ backgroundColor: "whiteSmoke" }} closeButton>
                            <Modal.Title>Confirm</Modal.Title>
                        </Modal.Header>
                        <Modal.Body style={{ backgroundColor: "whiteSmoke" }}>
                            <p>Are you sure you want to delete this user?</p>
                        </Modal.Body>
                        <Modal.Footer style={{ backgroundColor: "whiteSmoke" }}>
                            <Button onClick={() => this.setState({ showDeleteUserModal: false, userInAction: null })} variant="secondary">Cancel</Button>
                            <Button onClick={() => this.deleteAUser(this.state.userInAction)} variant="primary">Delete</Button>
                        </Modal.Footer>
                    </Modal>
                    <Modal show={this.state.showMakeUserAdminModal} onHide={() => this.setState({ showMakeUserAdminModal: false, userInAction: null })}>
                        <Modal.Header style={{ backgroundColor: "whiteSmoke" }} closeButton>
                            <Modal.Title>Confirm</Modal.Title>
                        </Modal.Header>
                        <Modal.Body style={{ backgroundColor: "whiteSmoke" }}>
                            <p>Are you sure you want to make this Non-Admin User an Admin User?</p>
                        </Modal.Body>
                        <Modal.Footer style={{ backgroundColor: "whiteSmoke" }}>
                            <Button onClick={() => this.setState({ showMakeUserAdminModal: false, userInAction: null })} variant="secondary">Cancel</Button>
                            <Button onClick={() => this.makeUserAdmin(this.state.userInAction)} variant="primary">Make Admin</Button>
                        </Modal.Footer>
                    </Modal>
                    <Modal show={this.state.showMakeAdminNonAdminModal} onHide={() => this.setState({ showMakeAdminNonAdminModal: false, userInAction: null })}>
                        <Modal.Header style={{ backgroundColor: "whiteSmoke" }} closeButton>
                            <Modal.Title>Confirm</Modal.Title>
                        </Modal.Header>
                        <Modal.Body style={{ backgroundColor: "whiteSmoke" }}>
                            <p>Are you sure you want to make this Admin User Non-Admin User?</p>
                        </Modal.Body>
                        <Modal.Footer style={{ backgroundColor: "whiteSmoke" }}>
                            <Button onClick={() => this.setState({ showMakeAdminNonAdminModal: false, userInAction: null })} variant="secondary">Cancel</Button>
                            <Button onClick={() => this.makeUserNonAdmin(this.state.userInAction)} variant="primary">Make Non-Admin</Button>
                        </Modal.Footer>
                    </Modal>

                    <Modal show={this.state.showAddUserModal} onHide={() => this.setState({ showAddUserModal: false, userInAction: null })}>
                        <Modal.Header style={{ backgroundColor: "whiteSmoke" }} closeButton>
                            <Modal.Title>Confirm</Modal.Title>
                        </Modal.Header>
                        <Modal.Body style={{ backgroundColor: "whiteSmoke" }}>
                            <p>Enter the email of the new user.</p>
                            <label htmlFor="email">Email address: </label>
                            <input type="email" id="email"></input>
                            <br></br><b id="error" style={{ color: "red" }}></b>
                        </Modal.Body>
                        <Modal.Footer style={{ backgroundColor: "whiteSmoke" }}>
                            <Button onClick={() => this.setState({ showAddUserModal: false, userInAction: null })} variant="secondary">Cancel</Button>
                            <Button onClick={() => {
                                    if (this.validateEmail(document.getElementById('email').value)) {
                                        document.getElementById('error').innerHTML = "";
                                        this.addUser(document.getElementById('email').value);
                                        this.setState({ showAddUserModal: false, userInAction: null })
                                }
                                else {
                                        document.getElementById('error').innerHTML = "Invalid Email";
                                }
                            }
                            } variant="primary">Add</Button>
                        </Modal.Footer>
                    </Modal>
                    <div style={{ display: "flex", justifyContent: "center" }}>
                        <h3>Admin Actions</h3>
                    </div>
                    <div style={{ display: "flex", justifyContent: "center" }}>
                        <Table striped bordered hover style={{ width: "85%" }}>
                            <thead>
                                <tr>
                                    <th>Actions</th>
                                    <th>Users</th>
                                    <th>Level</th>
                                </tr>
                            </thead>
                            <tbody>
                                {(this.state.adminUsers.length == 0 && this.state.nonAdminUsers.length == 0) &&
                                    <tr>
                                        <th>No Users (Only one main current Admin User).</th>
                                        <th>No Users (Only one main current Admin User).</th>
                                        <th>No Users (Only one main current Admin User).</th>
                                    </tr>
                                }
                                {this.state.adminUsers.length != 0 &&
                                    this.state.adminUsers.map(adminUsers =>
                                        <tr key={adminUsers.userName}>
                                            <td>{
                                                <React.Fragment>
                                                    <Link onClick={() => this.setState({ showDeleteUserModal: true, userInAction: adminUsers.userName })}>
                                                        <FontAwesomeIcon title="Delete User" style={{ color: "#73a353" }} className="fa-2x" icon={faTrash} />
                                                    </Link>
                                                    <Link onClick={() => this.setState({ showMakeAdminNonAdminModal: true, userInAction: adminUsers.userName })}>
                                                        <FontAwesomeIcon title="Make User Non-Admin" style={{ color: "#73a353", marginLeft: "15px" }} className="fa-2x" icon={faLevelDownAlt} />
                                                    </Link>
                                                </React.Fragment>
                                            }
                                            </td>
                                            <td>{adminUsers.userName}</td>
                                            <td>Admin User</td>
                                        </tr>
                                    )
                                }
                                {this.state.nonAdminUsers.length != 0 &&
                                    this.state.nonAdminUsers.map(nonAdminUsers =>
                                        <tr key={nonAdminUsers.userName}>
                                            <td>{
                                                <React.Fragment>
                                                    <Link onClick={() => this.setState({ showDeleteUserModal: true, userInAction: nonAdminUsers.userName })}>
                                                        <FontAwesomeIcon title="Delete User" style={{ color: "#73a353" }} className="fa-2x" icon={faTrash} />
                                                    </Link>
                                                    <Link onClick={() => this.setState({ showMakeUserAdminModal: true, userInAction: nonAdminUsers.userName })}>
                                                        <FontAwesomeIcon title="Make User Admin" style={{ color: "#73a353", marginLeft: "15px" }} className="fa-2x" icon={faLevelUpAlt} />
                                                    </Link>
                                                </React.Fragment>
                                            }
                                            </td>
                                            <td>{nonAdminUsers.userName}</td>
                                            <td>Normal User</td>
                                        </tr>
                                    )}
                                <tr>
                                    <th style={{ borderSpacing: "none", border: "none" }}></th>
                                    <th style={{ borderSpacing: "none", border: "none" }}></th>
                                    <th style={{ borderSpacing: "none", border: "none" }}></th>
                                </tr>
                            </tbody>
                        </Table>
                    </div>
                    <React.Fragment>
                        <Link onClick={() => this.setState({ showAddUserModal: true })}>
                            <FontAwesomeIcon title="Add New User" style={{ color: "#73a353", marginLeft: "8%" }} className="fa-2x" icon={faUserPlus} />
                        </Link>
                    </React.Fragment>
                </React.Fragment>
            );
        }
        return (
            <div>
                {content}
            </div>
        );
    }
}