import React, { Component } from 'react';
import { Table } from 'react-bootstrap';
import { Link } from 'react-router-dom';
import { Modal, Button } from 'react-bootstrap';
import authService from './api-authorization/AuthorizeService';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faSpinner, faTrash, faLevelUpAlt, faLevelDownAlt, faUserPlus, faCheck, faTimes, faFileDownload } from '@fortawesome/free-solid-svg-icons';
import { saveAs } from 'file-saver';

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
            showApproveModal: false,
            showRejectModal: false,
            showAddUserModal: false,
            userInAction: null,
            files: [],
            fileSelected: null,
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


        let getUnapprovedFiles = async () => {
            const token = await authService.getAccessToken();
            const response = await fetch('api/GreenWellFiles/UnapprovedFiles', {
                method: 'POST',
                headers: !token ? {} : { 'Authorization': `Bearer ${token}` }
            });
            const json = await response.json();
            if (json.files.length == 0) {
                this.setState({
                    files: [],
                });
            }
            else {
                var i;
                var t = [];
                for (i = 0; i < json.files.length; i++) {
                    var r1 = {
                        key: json.files[i]
                    };
                    t.push(r1);
                }
                this.setState({
                    files: t,
                });
            }
        }
        getUsers();
        getUnapprovedFiles();
    }

    resolveFile = async (fileToApprove, approval) => {
        const token = await authService.getAccessToken();
        let formData = new FormData();
        formData.append("fullPath", fileToApprove);
        formData.append("approval", approval);
        const response = await fetch('api/GreenWellFiles/ResolveApproval', {
            method: 'POST',
            body: formData,
            headers: !token ? {} : { 'Authorization': `Bearer ${token}` }
        });
        const json = await response.json();

        this.setState(state => {
            let newFiles = []
            state.files.map((file) => {
                if (file.key.fullPath !== fileToApprove) {
                    newFiles.push(file)
                }
            })
            state.files = newFiles;
            state.showApproveModal = false;
            state.showRejectModal = false;
            return state
        })
    };

    downloadFile = async (fileToDownload) => {
        let fileName = fileToDownload.split("/")[fileToDownload.split("/").length - 1];
        let formData = new FormData();
        formData.append("filePath", fileToDownload)
        const token = await authService.getAccessToken();
        const response = await fetch('api/GreenWellFiles/DownloadAFile', {
            method: 'POST',
            body: formData,
            headers: !token ? {} : { 'Authorization': `Bearer ${token}` }
        });

        const blob = await response.blob();
        saveAs(blob, fileName);

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
        if (/^[a-zA-Z0-9!#$%&'*+-/=?^_`{|]+@[a-zA-Z0-9]+\.[A-Za-z]+$/.test(email))
            return true;
        return false;
    }

    validateUsername = (username) => {
        if (/^[a-zA-Z0-9-._@+ ]+$/.test(username))
            return true;
        return false;

    }

    addUser = async (userToAdd, userName) => {
        const [user] = await Promise.all([authService.getUser()]);
        const token = await authService.getAccessToken();
        let formData = new FormData();
        formData.append("currentUser", user.name);
        formData.append("userEmail", userToAdd);
        formData.append("userName", userName);
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
                            <p>Are you sure you want to make this user an Admin?</p>
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
                            <p>Are you sure you want to remove this user's Admin status?</p>
                        </Modal.Body>
                        <Modal.Footer style={{ backgroundColor: "whiteSmoke" }}>
                            <Button onClick={() => this.setState({ showMakeAdminNonAdminModal: false, userInAction: null })} variant="secondary">Cancel</Button>
                            <Button onClick={() => this.makeUserNonAdmin(this.state.userInAction)} variant="primary">Make Non-Admin</Button>
                        </Modal.Footer>
                    </Modal>

                    <Modal show={this.state.showAddUserModal} onHide={() => this.setState({ showAddUserModal: false, userInAction: null })}>
                        <Modal.Header style={{ backgroundColor: "whiteSmoke" }} closeButton>
                            <Modal.Title>Add New User</Modal.Title>
                        </Modal.Header>
                        <Modal.Body style={{ backgroundColor: "whiteSmoke" }}>
                            <p>The user will receive an email instructing them how to finish setting up their account.</p>
                            <hr></hr>
                            <div>
                                <b>Name  </b>
                                <br></br>
                                <input type="name" id="name"></input>
                            </div>
                            <br></br>
                            <div>
                                <b>Email  </b>
                                <br></br>
                                <input type="email" id="email"></input>
                            </div>
                            <br></br><b id="error" style={{ color: "red" }}></b>
                        </Modal.Body>
                        <Modal.Footer style={{ backgroundColor: "whiteSmoke" }}>
                            <Button onClick={() => this.setState({ showAddUserModal: false, userInAction: null })} variant="secondary">Cancel</Button>
                            <Button onClick={() => {
                                if (this.validateEmail(document.getElementById('email').value)) {
                                    if (this.validateUsername(document.getElementById('name').value)) {
                                        document.getElementById('error').innerHTML = "";
                                        this.addUser(document.getElementById('email').value, document.getElementById('name').value);
                                        this.setState({ showAddUserModal: false, userInAction: null })
                                    }
                                    else {
                                        document.getElementById('error').innerHTML = "Invalid Name";
                                    }
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
                                    <th>Name</th>
                                    <th>Email</th>
                                    <th>Role</th>
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
                                        <tr key={adminUsers.email}>
                                            <td>{
                                                <React.Fragment>
                                                    <Link onClick={() => this.setState({ showDeleteUserModal: true, userInAction: adminUsers.email })}>
                                                        <FontAwesomeIcon title="Delete User" style={{ color: "#73a353" }} className="fa-2x" icon={faTrash} />
                                                    </Link>
                                                    <Link onClick={() => this.setState({ showMakeAdminNonAdminModal: true, userInAction: adminUsers.email })}>
                                                        <FontAwesomeIcon title="Remove Admin Role" style={{ color: "#73a353", marginLeft: "15px" }} className="fa-2x" icon={faLevelDownAlt} />
                                                    </Link>
                                                </React.Fragment>
                                            }
                                            </td>
                                            <td>{adminUsers.userName}</td>
                                            <td>{adminUsers.email}</td>
                                            <td>Admin User</td>
                                        </tr>
                                    )
                                }
                                {this.state.nonAdminUsers.length != 0 &&
                                    this.state.nonAdminUsers.map(nonAdminUsers =>
                                        <tr key={nonAdminUsers.userName}>
                                            <td>{
                                                <React.Fragment>
                                                    <Link onClick={() => this.setState({ showDeleteUserModal: true, userInAction: nonAdminUsers.email })}>
                                                        <FontAwesomeIcon title="Delete User" style={{ color: "#73a353" }} className="fa-2x" icon={faTrash} />
                                                    </Link>
                                                    <Link onClick={() => this.setState({ showMakeUserAdminModal: true, userInAction: nonAdminUsers.email })}>
                                                        <FontAwesomeIcon title="Make User Admin" style={{ color: "#73a353", marginLeft: "15px" }} className="fa-2x" icon={faLevelUpAlt} />
                                                    </Link>
                                                </React.Fragment>
                                            }
                                            </td>
                                            <td>{nonAdminUsers.userName}</td>
                                            <td>{nonAdminUsers.email}</td>
                                            <td>Default User</td>
                                        </tr>
                                    )}
                                <tr>
                                    <th style={{ borderSpacing: "none", border: "none" }}></th>
                                    <th style={{ borderSpacing: "none", border: "none" }}></th>
                                    <th style={{ borderSpacing: "none", border: "none" }}></th>
                                    <th style={{ borderSpacing: "none", border: "none" }}></th>
                                </tr>
                            </tbody>
                        </Table>
                    </div>
                    <Link onClick={() => this.setState({ showAddUserModal: true })}>
                        <FontAwesomeIcon title="Add New User" style={{ color: "#73a353", marginLeft: "8%" }} className="fa-2x" icon={faUserPlus} />
                    </Link>




                    <div style={{ display: "flex", justifyContent: "center" }}>
                        <h3>Approve Files</h3>
                    </div>

                    <Modal show={this.state.showApproveModal} onHide={() => this.setState({ showApproveModal: false, fileSelected: null })}>
                        <Modal.Header style={{ backgroundColor: "whiteSmoke" }} closeButton>
                            <Modal.Title>Confirm</Modal.Title>
                        </Modal.Header>
                        <Modal.Body style={{ backgroundColor: "whiteSmoke" }}>
                            <p>Are you sure you want to approve this file?</p>
                        </Modal.Body>
                        <Modal.Footer style={{ backgroundColor: "whiteSmoke" }}>
                            <Button onClick={() => this.setState({ showApproveModal: false, fileSelected: null })} variant="secondary">Cancel</Button>
                            <Button onClick={() => this.resolveFile(this.state.fileSelected, true)} variant="primary">Approve</Button>
                        </Modal.Footer>
                    </Modal>

                    <Modal show={this.state.showRejectModal} onHide={() => this.setState({ showRejectModal: false, fileSelected: null })}>
                        <Modal.Header style={{ backgroundColor: "whiteSmoke" }} closeButton>
                            <Modal.Title>Confirm</Modal.Title>
                        </Modal.Header>
                        <Modal.Body style={{ backgroundColor: "whiteSmoke" }}>
                            <p>Are you sure you want to reject this file, it will be permenently deleted from the server.</p>
                        </Modal.Body>
                        <Modal.Footer style={{ backgroundColor: "whiteSmoke" }}>
                            <Button onClick={() => this.setState({ showRejectModal: false, fileSelected: null })} variant="secondary">Cancel</Button>
                            <Button onClick={() => this.resolveFile(this.state.fileSelected, false)} variant="primary">Reject</Button>
                        </Modal.Footer>
                    </Modal>


                    <div style={{ display: "flex", justifyContent: "center" }}>
                        <Table striped bordered hover style={{ width: "75%" }}>
                            <thead>
                                <tr>
                                    <th>Actions</th>
                                    <th>Filename</th>
                                </tr>
                            </thead>
                            <tbody>
                                {(this.state.files.length == 0) &&
                                    <tr>
                                        <th>No files.</th>
                                        <th>No files.</th>
                                    </tr>
                                }

                                {this.state.files.length != 0 &&
                                    this.state.files.map(file =>
                                        <tr key={file.key.filename}>
                                            <td>{
                                                <React.Fragment>
                                                    <Link onClick={() => this.setState({ showApproveModal: true, fileSelected: file.key.fullPath })}>
                                                        <FontAwesomeIcon title="Approve File" style={{ color: "#73a353" }} className="fa-2x" icon={faCheck} />
                                                    </Link>
                                                    <Link onClick={() => this.setState({ showRejectModal: true, fileSelected: file.key.fullPath })}>
                                                        <FontAwesomeIcon title="Reject File" style={{ color: "#73a353", marginLeft: "15px" }} className="fa-2x" icon={faTimes} />
                                                    </Link>
                                                    <Link onClick={() => this.downloadFile(file.key.fullPath)}>
                                                        <FontAwesomeIcon title="Download File" style={{ color: "#73a353", marginLeft: "15px" }} className="fa-2x" icon={faFileDownload} />
                                                    </Link>
                                                </React.Fragment>
                                            }
                                            </td>
                                            <td>{file.key.filename}</td>

                                        </tr>
                                    )
                                }


                                <tr>
                                    <th style={{ borderSpacing: "none", border: "none" }}></th>
                                    <th style={{ borderSpacing: "none", border: "none" }}></th>
                                </tr>
                            </tbody>
                        </Table>
                    </div>
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