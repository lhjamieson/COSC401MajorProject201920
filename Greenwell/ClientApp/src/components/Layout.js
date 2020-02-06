import React, { Component } from 'react';
import { Col, Container, Row, Form, FormControl, Button, Image } from 'react-bootstrap';

export class Layout extends Component {
  displayName = Layout.name

    render() {
      return (
          <React.Fragment>
          <Container style={{padding: "0px"}} fluid>
          {this.props.children}
          </Container>
          </React.Fragment>
    );
  }
}

