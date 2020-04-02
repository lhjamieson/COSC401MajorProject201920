import React from 'react';
import { List, Datagrid, TextField, ReferenceField, EditButton, Edit, SimpleForm, TextInput, ReferenceInput, SelectInput, Create, EmailField } from 'react-admin';

export const PostList = props => (
    <List {...props}>
        <Datagrid>
            <ReferenceField source="userId" reference="users">
                <TextField source="name" />
            </ReferenceField>
            <TextField source="role" />
            <TextField source="email" />
            <EditButton />
        </Datagrid>
    </List>
);

export const PostEdit = props => (
    <Edit {...props}>
        <SimpleForm>
            <ReferenceInput source="userId" reference="users">
                <SelectInput optionText="name" />
            </ReferenceInput>
            <TextInput source="role" />
            <TextInput source="email" />
        </SimpleForm>
    </Edit>
);

export const PostCreate = props => (
    <Create {...props}>
        <SimpleForm>
            <ReferenceInput source="userId" reference="users">
                <SelectInput optionText="name" />
            </ReferenceInput>
            <TextInput source="role" />
            <TextInput source="email" />
        </SimpleForm>
    </Create>
);