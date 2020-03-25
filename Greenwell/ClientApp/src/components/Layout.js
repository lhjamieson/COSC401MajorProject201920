import React, { Component } from 'react';
import { NavMenu } from './NavMenu';
import { Col, Container, Row, Form, FormControl, Button, Image } from 'react-bootstrap';

export class Layout extends Component {
  static displayName = Layout.name;

  render () {
    return (
      <div>
        <NavMenu />
            <React.Fragment>
                <Container style={{ padding: "0px" }} fluid>
                    {this.props.children}
                </Container>
            </React.Fragment>
      </div>
    );
  }
}
