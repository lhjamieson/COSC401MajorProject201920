

import React, { Component } from "react";
import { Admin, Resource, EditGuesser } from "react-admin";
import { UserList } from './Users';
import jsonServerProvider from "ra-data-json-server";

const dataProvider =
    jsonServerProvider("https://jsonplaceholder.typicode.com");

class AdminAccess extends Component {
    render() {
        return (
            <Admin dataProvider={dataProvider}>
                <Resource
                    name="users"
                    list={UserList}
                    edit={EditGuesser}
                />
            </Admin>
        );
    }
}
export default AdminAccess;