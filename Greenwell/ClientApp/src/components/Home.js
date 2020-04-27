import React, { Component } from 'react';
import { Button, FormControl, Container, Navbar, Nav, InputGroup, Modal, Row, Image, Form, Dropdown, ButtonGroup, Col } from 'react-bootstrap';
import ReactDOM from 'react-dom';
import Moment from 'moment';
import { Typeahead } from 'react-bootstrap-typeahead';
import FileBrowser, { Icons } from 'react-keyed-file-browser';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faFile, faFolder, faImage, faFilePdf, faTrash, faFolderOpen, faFileSignature, faSpinner } from '@fortawesome/free-solid-svg-icons';
import '../react-keyed-file-browser.css';
import '../Typeahead.css';
import uploadButton from '../logos/upload.png';
import searchButton from '../logos/search.png';
import { saveAs } from 'file-saver';

import authService from './api-authorization/AuthorizeService';

export class Home extends Component {
    displayName = Home.name

    constructor(props) {
        super(props);
        this.state = {
            loading: true,
            //noFiles: false,
            //filesLoaded: false,
            files: [],
            showModal: false,
            uploadFile: null,
            uploadFileName: "",
            uploadFilePath: "",
            tagsForFileUpload: [],
            changeModalBody: false,
            tagsForFilter: [],
            showAlertModal: false,
            alertMessage: null,
            downloadFileName: "",
            uploading: false,
            searchBy: "fileName",
            role: null

        };
        // check and create the local storage
        this.createStorage();


        // check the state of the user and get the files
        this.populateState();
    }
    //Create storage on constructor
    async createStorage() {
        const token = await authService.getAccessToken();
        fetch('api/GreenWellFiles/CreateLocalStorage', {
            headers: !token ? {} : { 'Authorization': `Bearer ${token}` }
        });

    }

    // method that gets the state of the user and get the files accordingly
    async populateState() {
        let getFiles = async (r) => {
            const token = await authService.getAccessToken();
            const response = await fetch(r ? 'api/GreenWellFiles/AdminGetAllFiles' : 'api/GreenWellFiles/GetAllFiles', {
                method: 'POST',
                headers: !token ? {} : { 'Authorization': `Bearer ${token}` }
            });
            const json = await response.json();
            if (json.files.length == 0) {
                this.setState({
                    loading: false,
                    //noFiles: true,
                    //filesLoaded: false,
                    files: [],
                    tagsForFilter: []
                });
            }
            else {
                var i;
                var t = [];
                for (i = 0; i < json.files.length; i++) {
                    var r1 = {
                        key: json.files[i],
                        size: 1000,
                        modified: +Moment(),
                    };
                    t.push(r1);
                }
                this.setState({
                    loading: false,
                    //filesLoaded: true,
                    //noFiles: false,
                    files: t,
                    tagsForFilter: json.tags
                });
            }
        }

        // get the state of the user and pass role of the user to previous method to get files
        const [user] = await Promise.all([authService.getUser()]);
        this.setState({
            role: user && user.role
        }, () => getFiles(this.state.role == "Administrator"));
    }


    //Function that handles when a file is clicked on within react file browser
    handleFileSelection = (selection) => {
        this.setState({
            uploadFilePath: "",
            downloadFileName: selection.key
        });
    }

    //Function that handles when a folder is clicked on with react file browser
    handleFolderSelection = (selection) => {
        this.setState({
            uploadFilePath: selection.key,
            downloadFileName: ""
        });

    }


    handleCreateFolder = (key) => {
        // create object
        let formData = new FormData();
        formData.append("folderPath", key);
        //alert(key);
        // define async function
        let createFolder = async () => {
            const token = await authService.getAccessToken();
            const response = await fetch('api/GreenWellFiles/AddAFolder', {
                method: 'POST',
                body: formData,
                headers: !token ? {} : { 'Authorization': `Bearer ${token}` }
            });
            const json = await response.json();
            if (json.status === "200") {
                this.setState(state => {
                    state.files = state.files.concat([{
                        key: key,
                    }])
                    return state
                })
            }
            else {
                this.showAlertModal(json.message);
            }
        }
        // call it
        createFolder();
    }

    handleDeleteFolder = (folderKey) => {
        // get folder path string
        let formData = new FormData();
        formData.append("folderPath", folderKey.toString());
        // create async function
        let deleteFolder = async () => {
            const token = await authService.getAccessToken();
            const response = await fetch('api/GreenWellFiles/DeleteAFolder', {
                method: 'POST',
                body: formData,
                headers: !token ? {} : { 'Authorization': `Bearer ${token}` }
            })
            const json = await response.json();
            if (json.status === "200") {
                this.setState(state => {
                    const newFiles = []
                    state.files.map((file) => {
                        if (file.key.substr(0, folderKey.toString().length) != folderKey) {
                            newFiles.push(file);
                        }
                    });
                    //If a folder is deleted the upload path should reflect that.
                    state.uploadFilePath = "";
                    state.files = newFiles;
                    return state
                });
            }
            else {
                this.showAlertModal(json.message);
            }
        }
        // call it
        deleteFolder();
    }

    handleRenameFolder = (oldKey, newKey) => {
        // store old and new folder names
        if (oldKey.charAt(0) == "/") {
            oldKey = oldKey.substring(1, oldKey.length);
        }

        //If the user didn't rename the holder, we don't call the method.
        if (oldKey === newKey) {
            return
        }

        const rf = [oldKey, newKey]
        // create async function
        let renameFolder = async (p) => {
            const token = await authService.getAccessToken();
            const response = await fetch('api/GreenWellFiles/RenameAFolder', {
                method: 'POST',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify(p)
            })
            const json = await response.json();
            if (json.status === "200") {
                this.setState(state => {
                    const newFiles = []
                    state.files.map((file) => {
                        if (file.key.substr(0, oldKey.length) === oldKey) {
                            newFiles.push({
                                ...file,
                                key: file.key.replace(oldKey, newKey),
                            })
                        } else {
                            newFiles.push(file)
                        }
                    })
                    //If the folder is renamed, the uploadPath should reflect that.
                    state.uploadFilePath = newKey
                    state.files = newFiles
                    return state
                })
            }
            else {
                this.showAlertModal(json.message);
            }

        }
        // call it
        renameFolder(rf);
    }

    handleRenameFile = (oldKey, newKey) => {
        // store old and new file names
        const rf = [oldKey, newKey]

        //If the old name and the new name are the same, we don't call the method
        if (oldKey === newKey) {
            return
        }

        // create async function
        let renameFile = async (p) => {
            const token = await authService.getAccessToken();
            const response = await fetch('api/GreenWellFiles/RenameAFile', {
                method: 'POST',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify(p)
            })
            const json = await response.json();
            if (json.status === "200") {
                this.setState(state => {
                    this.setState(state => {
                        const newFiles = []
                        //Add all the new files to teh file browser
                        state.files.map((file) => {
                            if (file.key === oldKey) {
                                newFiles.push({
                                    ...file,
                                    key: newKey,
                                    size: 1000,
                                    modified: +Moment(),
                                })
                            } else {
                                newFiles.push(file)
                            }
                        })
                        state.files = newFiles;
                        //If the file name is altered, our selection should reflect that.
                        state.downloadFileName = newKey;
                        return state
                    })
                })
            }
            else {
                this.showAlertModal(json.message);
            }
        }
        // call it
        renameFile(rf);
    }


    handleDeleteFile = (fileKey) => {
        // store path for file to be deleted
        var df = fileKey.toString();
        // create async function
        let deleteFile = async (p) => {
            const token = await authService.getAccessToken();
            const response = await fetch('api/GreenWellFiles/DeleteAFile', {
                method: 'POST',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify(p)
            })
            const json = await response.json();
            if (json.status === "200") {
                this.setState(state => {
                    const newFiles = []
                    state.files.map((file) => {
                        if (file.key.toString() !== fileKey.toString()) {
                            newFiles.push(file)
                        }
                    })
                    state.files = newFiles
                    //If the file is deleted it can no longer be selected.
                    state.downloadFileName = ""
                    return state
                })
            }
            else {
                this.showAlertModal(json.message);
            }
        }
        // call it
        deleteFile(df);
    }

    handleModalClose = () => {
        this.setState({
            showModal: false,
            uploadFile: null,
            uploadFileName: "",
            uploadFilePath: "",
            uploading: false
        });
    }

    showAlertModal = (message) => {
        this.setState({
            showAlertModal: true,
            alertMessage: message
        })
    } 

    handleModalShow = () => {
        this.setState({
            showModal: true,
            uploading: true
        });
    }
    openBrowseDialog = (dialog) => {
        dialog.click();
    }

    handleSelectedFiles = (e) => {
        var files = e.currentTarget.files;
        //console.log(files[0]);
        this.setState({
            uploadFile: files[0],
            uploadFileName: files[0].name
        });

    }

    handleAddFileFromUpload = () => {
        let tags = [];
        if (!this.state.tagsForFileUpload.length == 0) {
            let i;
            for (i = 0; i < this.state.tagsForFileUpload.length; i++) {
                if (typeof this.state.tagsForFileUpload[i] === "object")
                    tags.push(this.state.tagsForFileUpload[i].label);
                else
                    tags.push(this.state.tagsForFileUpload[i]);
            }
        }

        let fullPath = this.state.uploadFilePath + this.state.uploadFileName;
        let addFile = async () => {
            let formData = new FormData();
            formData.append("path", fullPath)
            formData.append("f", this.state.uploadFile);
            formData.append("tags", tags);
            if (document.getElementById("adminCheckBox") == null)
                formData.append("adminAccessOnly", document.getElementById("adminCheckBox"));
            else
                formData.append("adminAccessOnly", document.getElementById("adminCheckBox").checked);

            const token = await authService.getAccessToken();
            const response = await fetch((this.state.role == "Administrator") ? 'api/GreenWellFiles/AdminAddFileFromUpload' : 'api/GreenWellFiles/AddFileFromUpload', {

                method: 'POST',
                body: formData,
                headers: !token ? {} : { 'Authorization': `Bearer ${token}` }
            });

            const json = await response.json();
            //Even if successful, we don't add the file to the browser if the user isn't an admin because it needs to be approved if they aren't an Administrator.
            if (json.status === "200") {
                //If they are an Administrator we add the uploaded file to the file browser
                if (this.state.role == "Administrator") {
                    this.setState(state => {
                        const addedNewFile = [];
                        addedNewFile.push({ 
                          key: this.state.uploadFilePath + this.state.uploadFileName,
                          size: 1000,
                          modified: +Moment(),
                        });

                        const uniqueNewFiles = []
                        addedNewFile.map((newFile) => {
                            let exists = false
                            state.files.map((existingFile) => {
                                if (existingFile.key === newFile.key) {
                                    exists = true
                                }
                            })
                            if (!exists) {
                                uniqueNewFiles.push(newFile)

                            }
                        })
                        state.files = state.files.concat(uniqueNewFiles)
                        return state

                    });
                    //Hide the upload modal
                    this.setState({
                        showModal: false,
                        uploadFile: null,
                        uploadFileName: "",
                        uploadFilePath: "",
                        uploading: false
                    });
                }
                //Otherwise we close the modal and tell the user the file has been sent to upload.
                else {
                    this.setState({
                        showModal: false,
                        uploadFile: null,
                        uploadFileName: "",
                        uploadFilePath: "",
                        uploading: false,
                    })
                    this.showAlertModal("File has been sent for approval");
                }
            }
            else if (json.status === "201") {
                this.setState({
                    showModal: false,
                    uploadFile: null,
                    uploadFileName: "",
                    uploadFilePath: "",
                    uploading: false,
                });
                this.showAlertModal("Unable to upload duplicate files.");
            }
        }
        addFile();
    }

    handleFileDownload = () => {
        // assign to variable
        //alert(this.state.downloadFileName);
        let filePath = this.state.downloadFileName;
        let fileName = filePath.split("/")[filePath.split("/").length - 1];
        let downloadFile = async () => {
            let formData = new FormData();
            formData.append("filePath", filePath)
            const token = await authService.getAccessToken();
            const response = await fetch('api/GreenWellFiles/DownloadAFile', {
                method: 'POST',
                body: formData,
                headers: !token ? {} : { 'Authorization': `Bearer ${token}` }
            });

            const blob = await response.blob();
            saveAs(blob, fileName);
        }
        downloadFile();
    }

    setFiles = (fs) => {
        var i;
        var t = [];
        for (i = 0; i < fs.length; i++) {
            var r1 = {
                key: fs[i],
                size: 1000,
                modified: +Moment(),
            };
            t.push(r1);
        }
        //console.log(t);
        this.setState({
            loading: false,
            //filesLoaded: true,
            //noFiles: false,
            files: t
        });
    }

    render() {
        const { loading, changeModalBody, uploadFile, uploadFileName, uploadFilePath, tagsForFilter } = this.state;
        let modalHeader;
        let modalBody;
        if (uploadFileName === "") {
            modalHeader = (
                <Modal.Header style={{ backgroundColor: "whiteSmoke" }} closeButton>
                    <Modal.Title>Upload A File.</Modal.Title>
                </Modal.Header>
            );
            modalBody = (
                null
            );
        }
        if (uploadFileName !== "") {
            modalHeader = (
                <Modal.Header style={{ backgroundColor: "whiteSmoke" }} closeButton>
                    <Modal.Title>{uploadFileName}</Modal.Title>
                </Modal.Header>
            );
            modalBody = (
                <Typeahead
                    id="tags"
                    onChange={(x) => this.setState({ tagsForFileUpload: x })}
                    multiple
                    allowNew
                    placeholder="Add Tags (optional)"
                    options={tagsForFilter}
                    selectHintOnEnter={true}
                    ref={(ref) => this._typeahead = ref}
                />
            );
        }
        let browseOrUpload;
        if (uploadFileName === "") {
            browseOrUpload = (
                <Button variant="secondary" onClick={() => this.openBrowseDialog(document.getElementById("dialog"))}>
                    Browse
                </Button>
            );
        }
        if (uploadFileName !== "") {
            browseOrUpload = (
                <Button variant="secondary" onClick={() => this.handleAddFileFromUpload()}>
                    Upload
                </Button>
            );
        }
        let adminFileCheck = null;
        let deletePermission = (
            <FileBrowser /*className="react-keyed-file-browser"*/
                files={this.state.files}
                icons={{
                    File: <FontAwesomeIcon className="fa-2x" icon={faFile} />,
                    Image: <FontAwesomeIcon className="fa-2x" icon={faImage} />,
                    PDF: <FontAwesomeIcon className="fa-2x" icon={faFilePdf} />,
                    Rename: <FontAwesomeIcon className="fa-2x" icon={faFileSignature} />,
                    Folder: <FontAwesomeIcon className="fa-2x" icon={faFolder} />,
                    FolderOpen: <FontAwesomeIcon className="fa-2x" icon={faFolderOpen} />,
                    Delete: <FontAwesomeIcon className="fa-2x" icon={faTrash} />,
                    Loading: <FontAwesomeIcon className="fa-2x" icon={faSpinner} />,
                }}
                onCreateFolder={this.handleCreateFolder}
                onRenameFolder={this.handleRenameFolder}
                onRenameFile={this.handleRenameFile}
                onSelectFile={this.handleFileSelection}
                onSelectFolder={this.handleFolderSelection}


            // onCreateFiles={this.handleCreateFiles}

            //onMoveFolder={this.handleRenameFolder}
            //onMoveFile={this.handleRenameFolder}
            />);
        if (this.state.role != null) {
            if (this.state.role == "Administrator") {
                deletePermission = (
                    <FileBrowser /*className="react-keyed-file-browser"*/
                        files={this.state.files}
                        icons={{
                            File: <FontAwesomeIcon className="fa-2x" icon={faFile} />,
                            Image: <FontAwesomeIcon className="fa-2x" icon={faImage} />,
                            PDF: <FontAwesomeIcon className="fa-2x" icon={faFilePdf} />,
                            Rename: <FontAwesomeIcon className="fa-2x" icon={faFileSignature} />,
                            Folder: <FontAwesomeIcon className="fa-2x" icon={faFolder} />,
                            FolderOpen: <FontAwesomeIcon className="fa-2x" icon={faFolderOpen} />,
                            Delete: <FontAwesomeIcon className="fa-2x" icon={faTrash} />,
                            Loading: <FontAwesomeIcon className="fa-2x" icon={faSpinner} />,
                        }}
                        onDeleteFolder={this.handleDeleteFolder}
                        onDeleteFile={this.handleDeleteFile}
                        onCreateFolder={this.handleCreateFolder}
                        onRenameFolder={this.handleRenameFolder}
                        onRenameFile={this.handleRenameFile}
                        onSelectFile={this.handleFileSelection}
                        onSelectFolder={this.handleFolderSelection}


                    // onCreateFiles={this.handleCreateFiles}

                    //onMoveFolder={this.handleRenameFolder}
                    //onMoveFile={this.handleRenameFolder}
                    />
                );

                if (uploadFileName !== "") {
                    adminFileCheck = (
                        <div>
                            <Form.Check id="adminCheckBox" type="checkbox" label="Show file to Admin Users only." />
                            <br />
                        </div>
                    );
                }
            }
        }
        let content;
        if (loading) {
            content =
                (
                    <div style={{ paddingLeft: "40px", marginTop: "20px" }}>
                        <h1>Loading</h1>
                        < FontAwesomeIcon className="fa-2x" icon={faSpinner} pulse />
                    </div>
                );
        }
        let download = (
            <div style={{ textAlign: "right" }}>
                <Button onClick={this.handleFileDownload}>Download</Button>
            </div>
        );
        if (this.state.downloadFileName.trim() === "") {
            download = (
                <div style={{ textAlign: "right" }}>
                    <Button style={{ cursor: "default", backgroundColor: "gray" }}>Download</Button>
                </div>
            );
        }
        //    if (noFiles && !loading) {
        //        content =
        //            (
        //                <Container style={{ marginTop: "18%", padding: "0px", marginLeft: "32%", marginRight: "0px" }}>
        //                    <InputGroup>
        //                        <Button onClick={() => this.fetchPathFiles(document.getElementById("inputPath").value)}>Fetch</Button>
        //                        <FormControl id="inputPath" style={{ maxWidth: "40%" }}
        //                            required
        //                            placeholder=":Path"
        //                            aria-describedby="basic-addon1"
        //                        />
        //                    </InputGroup>
        //                </Container>
        //            );

        //}

        if (!loading) {
            content =
                (
                    <React.Fragment>
                        <div id="file_browser" className="div-files">
                            {deletePermission}
                        </div>
                        {download}
                        <Modal show={this.state.showModal} onHide={this.handleModalClose}>
                            {modalHeader}
                            {modalBody}
                            <Modal.Footer style={{ backgroundColor: "whiteSmoke" }}>

                                <Container>
                                    <Row>
                                        <Col style={{ width: "100%", textAlign: "center" }}>
                                            {adminFileCheck}
                                        </Col>
                                    </Row>
                                    <Row>
                                        <Col style={{ width: "50%", textAlign: "center" }}>
                                            <Button variant="secondary" onClick={this.handleModalClose}>
                                                Close
                                        </Button>
                                        </Col>
                                        <Col style={{ width: "50%", textAlign: "center" }}>
                                            {browseOrUpload}
                                        </Col>
                                    </Row>
                                </Container>
                                <Form.Control id="dialog" onChange={this.handleSelectedFiles} type="file"></Form.Control>
                            </Modal.Footer>
                        </Modal>
                        <Footer handleModalShow={this.handleModalShow} />

                        <Modal centered show={this.state.showAlertModal} onEnter={() => { document.getElementById("alert").innerHTML = this.state.alertMessage }} onHide={() => this.setState({ showAlertModal: false, alertMesssage: null })}>
                            <Modal.Body style={{ backgroundColor: "whiteSmoke" }}>
                                <p id="alert"></p>
                            </Modal.Body>
                            <Modal.Footer style={{ backgroundColor: "whiteSmoke" }}>
                                <Button onClick={() => { this.setState({ showAlertModal: false, alertMesssage: null }) }} variant="primary">Ok</Button>
                            </Modal.Footer>
                        </Modal>


                    </React.Fragment>
                );
        }
        return (
            <div>
                <GreenWellNavMenu setParentForFiles={this.setFiles} />
                {content}
            </div>
        );
    }
}

class GreenWellNavMenu extends Component {
    displayName = GreenWellNavMenu.name

    constructor(props) {
        super(props);
        this.state = {
            role: null,
            searchBy: "fileName"
        };
        this.populateState();
    }

    async populateState() {
        const [user] = await Promise.all([authService.getUser()])
        this.setState({
            role: user && user.role
        });
    }

    Search = (val) => {
        let search = async (p) => {
            let data = [p, this.state.searchBy, this.state.role];
            const token = await authService.getAccessToken();
            const response = await fetch((this.state.role == "Administrator") ? 'api/GreenWellFiles/AdminSearch' : 'api/GreenWellFiles/Search', {

                method: 'POST',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`

                },
                body: JSON.stringify(data)
            });
            const json = await response.json();
            this.props.setParentForFiles(json.files);
        }
        search(val);
    }

    setSearchBy = (b) => {
        if (document.getElementById("search").value.toString().trim() !== "") {
            this.setState({
                searchBy: b
            }, () => this.Search(document.getElementById("search").value.toString().trim()));
        }
        else {
            this.setState({
                searchBy: b
            });
        }
    }

    render() {
        return (
            <Navbar className="GreenWell-Nav-Menu">
                <Nav className="ml-auto" /*style={{ paddingTop: "5px" }}*/ >
                    <Dropdown onSelect={(evt) => this.setSearchBy(evt)} as={ButtonGroup}>
                        <Dropdown.Toggle id="dropdown" />
                        <Dropdown.Menu>
                            <Dropdown.Item className="drop-down-item-style" eventKey="fileName" active={this.state.searchBy === "fileName"}>Search By File Name</Dropdown.Item>
                            <Dropdown.Item className="drop-down-item-style" eventKey="tags" active={this.state.searchBy === "tags"}> By Tags</Dropdown.Item>
                        </Dropdown.Menu>
                    </Dropdown>
                    <Form inline>
                        <FormControl id="search" onKeyPress={event => { if (event.key === "Enter") { event.preventDefault(); }}} onChange={() => this.Search(document.getElementById("search").value)} style={{ height: "45px", backgroundColor: "transparent", border: "2px solid white" }} type="text" placeholder="Search" />
                        <Button onClick={() => this.Search(document.getElementById("search").value)} className="search-button">
                            <Image src={searchButton} />
                        </Button>
                    </Form>
                </Nav>
            </Navbar>
        );
    }
}

class UploadButton extends Component {
    render() {
        return (
            <Row style={{ marginTop: "15px", float: "right", paddingRight: "50px" }} className="ml-auto">
                <button onClick={this.props.handleModalShow} style={{ paddingRight: "50px", width: "50px", border: "none" }} className="upload-Button1" >
                    <Image src={uploadButton} />
                </button>
                <button onClick={this.props.handleModalShow} style={{ border: "none", marginLeft: "2px" }} className="upload-Button2" >Upload</button>
            </Row>
        );
    }
}

class Footer extends Component {
    render() {
        return (
            <div className="upload-footer">
                <UploadButton handleModalShow={this.props.handleModalShow} />
            </div>
        );
    }
}
